package com.wanggaspa.receipt

import android.Manifest
import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothManager
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import android.provider.Settings
import androidx.activity.ComponentActivity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.unit.dp
import androidx.core.content.ContextCompat
import androidx.room.Room
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent { MaterialTheme { CashierScreen() } }
    }
}

fun btPermissions(): Array<String> =
    if (Build.VERSION.SDK_INT >= 31)
        arrayOf(Manifest.permission.BLUETOOTH_SCAN, Manifest.permission.BLUETOOTH_CONNECT)
    else
        arrayOf(Manifest.permission.BLUETOOTH, Manifest.permission.BLUETOOTH_ADMIN,
            Manifest.permission.ACCESS_FINE_LOCATION)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CashierScreen() {
    val ctx = LocalContext.current
    val scope = rememberCoroutineScope()
    var customer by remember { mutableStateOf("Mama Isaac") }
    var nomorHp by remember { mutableStateOf("") }
    var promo by remember { mutableStateOf("") }
    var services by remember { mutableStateOf(listOf<Svc>()) }
    val cart = remember { mutableStateListOf<ReceiptItem>() }
    var selService by remember { mutableStateOf(0) }
    var qtyText by remember { mutableStateOf("1") }
    var pay by remember { mutableStateOf("LUNAS Qris") }
    var discount by remember { mutableStateOf("") }
    var discountType by remember { mutableStateOf("%") }
    var preview by remember { mutableStateOf("") }
    var status by remember { mutableStateOf("") }
    var tab by remember { mutableStateOf(0) }
    // 0=Kasir 1=Riwayat 2=Master
    var history by remember { mutableStateOf(listOf<Tx>()) }
    var dailyTotal by remember { mutableStateOf(0L) }
    val db = remember {
        Room.databaseBuilder(ctx, AppDb::class.java, "wangga.db")
            .fallbackToDestructiveMigration().build()
    }

    // ---- Bluetooth state ----
    var btGranted by remember { mutableStateOf(false) }
    var devices by remember { mutableStateOf(listOf<BluetoothDevice>()) }
    var selectedAddr by remember { mutableStateOf<String?>(null) }
    var btMenuOpen by remember { mutableStateOf(false) }
    var btOn by remember { mutableStateOf(false) }

    fun hasBtPermission() = btPermissions().all {
        ContextCompat.checkSelfPermission(ctx, it) == PackageManager.PERMISSION_GRANTED
    }

    fun adapter(): BluetoothAdapter? =
        ctx.getSystemService(BluetoothManager::class.java)?.adapter

    fun safeName(d: BluetoothDevice) = try { d.name ?: d.address } catch (e: SecurityException) { d.address }

    fun loadDevices() {
        if (!hasBtPermission()) { btGranted = false; devices = emptyList(); return }
        btGranted = true
        val a = adapter()
        btOn = a?.isEnabled == true
        val bonded = try { a?.bondedDevices?.toList() ?: emptyList() } catch (e: SecurityException) { emptyList() }
        devices = bonded.sortedBy { safeName(it) }
        if (selectedAddr == null || bonded.none { it.address == selectedAddr }) {
            selectedAddr = bonded.firstOrNull {
                val n = try { it.name ?: "" } catch (e: SecurityException) { "" }
                n.contains("583", true) || n.contains("EXP", true) ||
                    n.contains("POS", true) || n.contains("printer", true)
            }?.address ?: bonded.firstOrNull()?.address
        }
    }

    val permLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestMultiplePermissions()
    ) { loadDevices() }

    val enableLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.StartActivityForResult()
    ) { loadDevices() }

    fun ensurePermissionThen(op: () -> Unit) {
        if (hasBtPermission()) { loadDevices(); op() }
        else permLauncher.launch(btPermissions())
    }

    fun selectedDevice(): BluetoothDevice? =
        devices.firstOrNull { it.address == selectedAddr }

    fun refreshHistory() {
        scope.launch {
            val all = withContext(Dispatchers.IO) { db.tx().all() }
            history = all
            val today = SimpleDateFormat("dd/MM/yyyy", Locale.US).format(Date())
            dailyTotal = withContext(Dispatchers.IO) { db.tx().dailyTotal(today) }
        }
    }

    fun refreshServices() {
        scope.launch {
            services = withContext(Dispatchers.IO) { db.svc().all() }
            val today = SimpleDateFormat("dd/MM/yyyy", Locale.US).format(Date())
            dailyTotal = withContext(Dispatchers.IO) { db.tx().dailyTotal(today) }
        }
    }

    fun cartSubtotal() = cart.sumOf { it.lineTotal }
    fun cartGrand() = cartSubtotal() - ReceiptTextFormatter.discountAmount(
        cartSubtotal(), if (discount.isBlank()) "" else discountType,
        discount.trim().toLongOrNull() ?: 0L)

    fun addToCart() {
        val svc = services.getOrNull(selService) ?: return
        val q = qtyText.trim().toIntOrNull() ?: 1
        if (q < 1) { status = "Qty harus >= 1"; return }
        cart.removeAll { it.name == svc.name }
        cart.add(ReceiptItem(svc.name, svc.desc, svc.price).apply { qty = q })
        status = "Ditambah: ${svc.name} x$q"
    }

    fun buildReceipt(): Receipt {
        val sdf = SimpleDateFormat("dd/MM/yyyy", Locale.US)
        val stf = SimpleDateFormat("HH:mm", Locale.US)
        val now = Date()
        val discVal = discount.trim().toLongOrNull() ?: 0L
        val discType = if (discount.trim().isEmpty()) "" else discountType
        return Receipt(
            StoreInfo("WANGGA SPA", "Sehat * Relaks * Bahagia",
                "Kayu Putih II No.32, Pulo Gadung, Jaktim",
                "Telp: 08211347294 | IG: @wangggasbabymomwoman"),
            customer, nomorHp, promo, discType, discVal, sdf.format(now), stf.format(now) + " WIB",
            cart.toList(), pay,
            listOf("TERIMA KASIH ATAS KUNJUNGAN ANDA",
                "Kesehatan & Kebugaran Prioritas",
                "***Wangga Baby Mom Woman Spa***")
        )
    }

    LaunchedEffect(Unit) {
        if (hasBtPermission()) loadDevices()
        scope.launch {
            val seed = listOf(
                Svc("Massage Kids", "Durasi 60 menit", 135000),
                Svc("Inflaren", "Durasi 30 menit", 50000),
                Svc("Transport PP (HM Care)", "Jarak & antar jemput", 15000))
            withContext(Dispatchers.IO) { seed.forEach { db.svc().upsert(it) } }
            refreshServices()
        }
    }

    Column(Modifier.padding(12.dp).verticalScroll(rememberScrollState())) {
        Image(painterResource(R.drawable.logo_wangga), contentDescription = "Wangga Spa",
            modifier = Modifier.fillMaxWidth().height(72.dp), contentScale = ContentScale.Fit)
        Text("Kasir 58mm", style = MaterialTheme.typography.headlineSmall)
        Row(Modifier.padding(top = 4.dp), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            Button(onClick = { tab = 0 }) { Text("Kasir") }
            Button(onClick = { tab = 1; refreshHistory() }) { Text("Riwayat") }
            Button(onClick = { tab = 2; refreshServices() }) { Text("Master") }
        }
        if (tab == 1) {
            Text("Hari ini: Rp $dailyTotal — ${history.size} transaksi",
                style = MaterialTheme.typography.labelLarge, modifier = Modifier.padding(top = 8.dp))
            history.forEach { t ->
                Text("${t.date} ${t.time} | ${t.customer} | Rp ${t.total} | ${t.paymentStatus}",
                    modifier = Modifier.padding(top = 2.dp))
            }
        } else if (tab == 0) {
        // ---- Printer card ----
        Text("Printer Bluetooth", style = MaterialTheme.typography.labelLarge, modifier = Modifier.padding(top = 8.dp))
        if (!btGranted) {
            Text("Izin Bluetooth belum diberikan. Aplikasi butuh izin untuk mencari printer.",
                modifier = Modifier.padding(top = 4.dp))
            Button(onClick = { permLauncher.launch(btPermissions()) },
                modifier = Modifier.padding(top = 4.dp)) { Text("Minta Izin Bluetooth") }
        } else {
            if (!btOn) {
                Text("Bluetooth HP mati.", modifier = Modifier.padding(top = 4.dp))
                Button(onClick = { enableLauncher.launch(Intent(BluetoothAdapter.ACTION_REQUEST_ENABLE)) },
                    modifier = Modifier.padding(top = 4.dp)) { Text("Nyalakan Bluetooth") }
            }
            ExposedDropdownMenuBox(expanded = btMenuOpen, onExpandedChange = { btMenuOpen = it }) {
                OutlinedTextField(
                    value = selectedDevice()?.let { "${safeName(it)}" } ?: "- pilih printer -",
                    onValueChange = {}, readOnly = true, label = { Text("Printer") },
                    trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(btMenuOpen) },
                    modifier = Modifier.menuAnchor().fillMaxWidth())
                ExposedDropdownMenu(expanded = btMenuOpen, onDismissRequest = { btMenuOpen = false }) {
                    devices.forEach { d ->
                        DropdownMenuItem(
                            text = { Text("${safeName(d)}\n${d.address}") },
                            onClick = { selectedAddr = d.address; btMenuOpen = false })
                    }
                }
            }
            Row(Modifier.padding(top = 4.dp), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                Button(onClick = { loadDevices() }) { Text("Refresh") }
                Button(onClick = { ctx.startActivity(Intent(Settings.ACTION_BLUETOOTH_SETTINGS)) }) { Text("Pair Baru") }
            }
            if (devices.isEmpty())
                Text("Belum ada printer paired. Klik Pair Baru, pair EXP583, lalu Refresh.",
                    modifier = Modifier.padding(top = 4.dp))
        }
        OutlinedTextField(customer, { customer = it }, label = { Text("Pelanggan") }, modifier = Modifier.fillMaxWidth())
        OutlinedTextField(nomorHp, { nomorHp = it }, label = { Text("Nomor HP") }, modifier = Modifier.fillMaxWidth())
        OutlinedTextField(promo, { promo = it }, label = { Text("Promo (kode, boleh kosong)") }, modifier = Modifier.fillMaxWidth())
        Row(verticalAlignment = Alignment.CenterVertically) {
            OutlinedTextField(discount, { discount = it }, label = { Text("Diskon (kosong = tanpa diskon)") },
                modifier = Modifier.weight(1f))
            Spacer(Modifier.width(8.dp))
            Button(onClick = { discountType = "%" }, enabled = discountType != "%") { Text("%") }
            Spacer(Modifier.width(4.dp))
            Button(onClick = { discountType = "Rp" }, enabled = discountType != "Rp") { Text("Rp") }
        }
        Text("Tambah layanan", style = MaterialTheme.typography.labelLarge, modifier = Modifier.padding(top = 8.dp))
        if (services.isEmpty()) {
            Text("Belum ada layanan. Tambahkan di tab Master.", modifier = Modifier.padding(top = 4.dp))
        } else {
            Row(verticalAlignment = Alignment.CenterVertically) {
                OutlinedTextField(
                    value = services[selService.coerceIn(0, services.size - 1)].name,
                    onValueChange = {}, readOnly = true, label = { Text("Layanan") },
                    modifier = Modifier.weight(1f))
                Spacer(Modifier.width(8.dp))
                Button(onClick = { selService = (selService - 1).mod(services.size) }) { Text("<") }
                Text("${selService + 1}/${services.size}")
                Button(onClick = { selService = (selService + 1).mod(services.size) }) { Text(">") }
                Spacer(Modifier.width(8.dp))
                OutlinedTextField(qtyText, { qtyText = it }, label = { Text("Qty") },
                    modifier = Modifier.width(72.dp))
                Button(onClick = { addToCart() }) { Text("Tambah") }
            }
        }
        cart.forEach { item ->
            Row(Modifier.fillMaxWidth().padding(top = 2.dp), verticalAlignment = Alignment.CenterVertically) {
                Text("${item.label} — ${ReceiptTextFormatter.rupiah(item.lineTotal)}",
                    modifier = Modifier.weight(1f))
                Button(onClick = { cart.remove(item) }) { Text("Hapus") }
            }
        }
        if (cart.isNotEmpty()) {
            Text("Subtotal: ${ReceiptTextFormatter.rupiah(cartSubtotal())} — " +
                 "TOTAL: ${ReceiptTextFormatter.rupiah(cartGrand())}",
                style = MaterialTheme.typography.labelLarge, modifier = Modifier.padding(top = 4.dp))
        }
        OutlinedTextField(pay, { pay = it }, label = { Text("Status Pembayaran") }, modifier = Modifier.fillMaxWidth())
        Row(Modifier.padding(top = 8.dp), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            Button(onClick = { preview = ReceiptTextFormatter.buildPreview(buildReceipt()) }) { Text("Preview") }
            Button(onClick = {
                scope.launch {
                    status = try {
                        if (!hasBtPermission()) throw Exception("Izin Bluetooth belum diberikan.")
                        val dev = selectedDevice() ?: throw Exception("Pilih printer dulu.")
                        val svc = BluetoothPrinterService(adapter())
                        val r = buildReceipt()
                        withContext(Dispatchers.IO) {
                            svc.print(dev, EscPosBuilder.build(r))
                            db.tx().insert(Tx(date = r.date, time = r.time,
                                customer = r.customer, customerPhone = r.customerPhone,
                                promoPhone = r.promoCode,
                                discountType = r.discountType, discountValue = r.discountValue,
                                itemsJson = ReceiptJson.itemsToJson(r.items),
                                subtotal = r.subtotal, total = r.grandTotal,
                                paymentStatus = r.paymentStatus))
                        }
                        refreshHistory()
                        "Tercetak + tersimpan ke ${safeName(dev)}"
                    } catch (e: Exception) { "Gagal: ${e.message}" }
                }
            }) { Text("Print") }
            Button(onClick = {
                scope.launch {
                    status = try {
                        if (!hasBtPermission()) throw Exception("Izin Bluetooth belum diberikan.")
                        val dev = selectedDevice() ?: throw Exception("Pilih printer dulu.")
                        val svc = BluetoothPrinterService(adapter())
                        val r = buildReceipt()
                        preview = ReceiptTextFormatter.buildPreview(r)
                        withContext(Dispatchers.IO) { svc.print(dev, EscPosBuilder.build(r)) }
                        "Test terkirim ke ${safeName(dev)} (isi sesuai form)"
                    } catch (e: Exception) { "Gagal: ${e.message}" }
                }
            }) { Text("Test") }
        }
        Text(status, modifier = Modifier.padding(top = 4.dp))
        Text("Preview 58mm (32 kolom):", style = MaterialTheme.typography.labelLarge, modifier = Modifier.padding(top = 8.dp))
        Text(preview, fontFamily = FontFamily.Monospace, modifier = Modifier.padding(top = 4.dp))
        } else if (tab == 2) {
            var mName by remember { mutableStateOf("") }
            var mDesc by remember { mutableStateOf("") }
            var mPrice by remember { mutableStateOf("") }
            var mStatus by remember { mutableStateOf("") }
            Text("Master Layanan", style = MaterialTheme.typography.headlineSmall)
            OutlinedTextField(mName, { mName = it }, label = { Text("Layanan") }, modifier = Modifier.fillMaxWidth())
            OutlinedTextField(mDesc, { mDesc = it }, label = { Text("Deskripsi") }, modifier = Modifier.fillMaxWidth())
            OutlinedTextField(mPrice, { mPrice = it }, label = { Text("Harga") }, modifier = Modifier.fillMaxWidth())
            Row(Modifier.padding(top = 4.dp), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                Button(onClick = {
                    scope.launch {
                        val price = mPrice.trim().toLongOrNull()
                        when {
                            mName.isBlank() -> mStatus = "Nama layanan wajib diisi"
                            price == null -> mStatus = "Harga harus angka"
                            else -> {
                                withContext(Dispatchers.IO) {
                                    db.svc().upsert(Svc(mName.trim(), mDesc.trim(), price))
                                }
                                mStatus = "Tersimpan: ${mName.trim()}"
                                mName = ""; mDesc = ""; mPrice = ""
                                refreshServices()
                            }
                        }
                    }
                }) { Text("Simpan") }
                Button(onClick = {
                    scope.launch {
                        if (mName.isNotBlank()) {
                            withContext(Dispatchers.IO) { db.svc().delete(mName.trim()) }
                            val gone = cart.removeAll { it.name == mName.trim() }
                            mStatus = if (gone) "Dihapus: ${mName.trim()} (juga dari keranjang)"
                                      else "Dihapus: ${mName.trim()}"
                            mName = ""; mDesc = ""; mPrice = ""
                            refreshServices()
                        }
                    }
                }) { Text("Hapus") }
            }
            Text(mStatus, modifier = Modifier.padding(top = 4.dp))
            services.forEach { svc ->
                Text("${svc.name} | ${svc.desc} | ${ReceiptTextFormatter.rupiah(svc.price)}",
                    modifier = Modifier.padding(top = 2.dp))
            }
        }
    }
}
