package com.wanggaspa.receipt

import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothManager
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.unit.dp
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

@Composable
fun CashierScreen() {
    val ctx = LocalContext.current
    val scope = rememberCoroutineScope()
    var customer by remember { mutableStateOf("Mama Isaac") }
    var promo by remember { mutableStateOf("08131006650") }
    var itemsText by remember { mutableStateOf("Massage Kids | Durasi 60 menit | 135000\nInflaren | Durasi 30 menit | 50000\nTransport PP (HM Care) | Jarak & antar jemput | 15000") }
    var pay by remember { mutableStateOf("LUNAS Qris") }
    var preview by remember { mutableStateOf("") }
    var status by remember { mutableStateOf("") }

    fun buildReceipt(): Receipt {
        val sdf = SimpleDateFormat("dd/MM/yyyy", Locale.US)
        val stf = SimpleDateFormat("HH:mm", Locale.US)
        val now = Date()
        val items = itemsText.lines().mapNotNull { ln ->
            val p = ln.split("|")
            if (p.size < 3) null else ReceiptItem(p[0].trim(), p[1].trim(), p[2].trim().toLongOrNull() ?: 0L)
        }
        val total = items.sumOf { it.price }
        return Receipt(
            StoreInfo("WANGGA SPA", "Sehat * Relaks * Bahagia",
                "Kayu Putih II No.32, Pulo Gadung, Jaktim",
                "Telp: 08211347294 | IG: @wangggasbabymomwoman"),
            customer, promo, sdf.format(now), stf.format(now) + " WIB",
            items, total, total, pay,
            listOf("TERIMA KASIH ATAS KUNJUNGAN ANDA",
                "Kesehatan & Kebugaran Prioritas Kami",
                "*** Wangga Baby Mom Woman Spa ***")
        )
    }

    Column(Modifier.padding(12.dp).verticalScroll(rememberScrollState())) {
        Text("WANGGA SPA — Kasir", style = MaterialTheme.typography.headlineSmall)
        OutlinedTextField(customer, { customer = it }, label = { Text("Pelanggan") }, modifier = Modifier.fillMaxWidth())
        OutlinedTextField(promo, { promo = it }, label = { Text("Promo (HP)") }, modifier = Modifier.fillMaxWidth())
        OutlinedTextField(itemsText, { itemsText = it }, label = { Text("Layanan | Deskripsi | Harga") },
            modifier = Modifier.fillMaxWidth().height(140.dp))
        OutlinedTextField(pay, { pay = it }, label = { Text("Status Pembayaran") }, modifier = Modifier.fillMaxWidth())
        Row(Modifier.padding(top = 8.dp), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            Button(onClick = { preview = ReceiptTextFormatter.buildPreview(buildReceipt()) }) { Text("Preview") }
            Button(onClick = {
                scope.launch {
                    status = try {
                        val bm = ctx.getSystemService(BluetoothManager::class.java)
                        val svc = BluetoothPrinterService(bm?.adapter)
                        val dev = svc.bondedPrinters().firstOrNull {
                            it.name?.contains("583", true) == true || it.name?.contains("EXP", true) == true
                        } ?: throw Exception("EXP583 belum paired. Pair dulu di Settings > Bluetooth.")
                        withContext(Dispatchers.IO) { svc.print(dev, EscPosBuilder.build(buildReceipt())) }
                        "Tercetak ke ${dev.name}"
                    } catch (e: Exception) { "Gagal: ${e.message}" }
                }
            }) { Text("Print") }
            Button(onClick = {
                scope.launch {
                    status = try {
                        val bm = ctx.getSystemService(BluetoothManager::class.java)
                        val svc = BluetoothPrinterService(bm?.adapter)
                        val dev = svc.bondedPrinters().firstOrNull()
                            ?: throw Exception("Tidak ada printer paired.")
                        withContext(Dispatchers.IO) { svc.print(dev, EscPosBuilder.build(sampleReceipt())) }
                        "Test terkirim ke ${dev.name}"
                    } catch (e: Exception) { "Gagal: ${e.message}" }
                }
            }) { Text("Test") }
        }
        Text(status, modifier = Modifier.padding(top = 4.dp))
        Text("Preview 58mm (32 kolom):", style = MaterialTheme.typography.labelLarge, modifier = Modifier.padding(top = 8.dp))
        Text(preview, fontFamily = FontFamily.Monospace, modifier = Modifier.padding(top = 4.dp))
        Text("Bluetooth: gunakan nama EXP583 / 583. Adapter state: " +
            (ctx.getSystemService(BluetoothManager::class.java)?.adapter?.isEnabled?.toString() ?: "?"))
    }
}
