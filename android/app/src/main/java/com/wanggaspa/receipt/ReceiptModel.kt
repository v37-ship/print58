package com.wanggaspa.receipt

/** One receipt line. Qty > 1 prints as "Name xN". */
data class ReceiptItem(val name: String, val desc: String, val price: Long) {
    var qty: Int = 1
    val lineTotal: Long get() = price * qty
    val label: String get() = if (qty > 1) "$name x$qty" else name
}

data class StoreInfo(val name: String, val tagline: String, val address: String, val contact: String)
data class Receipt(
    val store: StoreInfo,
    val customer: String,
    val customerPhone: String,
    val promoCode: String,
    val discountType: String,
    val discountValue: Long,
    val date: String,
    val time: String,
    val items: List<ReceiptItem>,
    val paymentStatus: String,
    val footerLines: List<String>
) {
    /** Sum of line totals — single source of truth, so callers cannot disagree. */
    val subtotal: Long get() = items.sumOf { it.lineTotal }

    /** Payable amount after discount. Never negative. */
    val grandTotal: Long get() = subtotal - ReceiptTextFormatter.discountAmount(subtotal, discountType, discountValue)
}

fun sampleReceipt() = Receipt(
    StoreInfo("WANGGA SPA", "Sehat * Relaks * Bahagia",
        "Kayu Putih II No.32, Pulo Gadung, Jaktim",
        "Telp: 08211347294 | IG: @wangggasbabymomwoman"),
    "Mama Isaac", "08131006650", "", "", 0L, "28/09/2026", "14:15 WIB",
    listOf(
        ReceiptItem("Massage Kids", "Durasi 60 menit", 135000),
        ReceiptItem("Inflaren", "Durasi 30 menit", 50000),
        ReceiptItem("Transport PP (HM Care)", "Jarak & antar jemput", 15000)
    ),
    "LUNAS Qris",
    listOf(
        "TERIMA KASIH ATAS KUNJUNGAN ANDA",
        "Kesehatan & Kebugaran Prioritas",
        "***Wangga Baby Mom Woman Spa***"
    )
)
