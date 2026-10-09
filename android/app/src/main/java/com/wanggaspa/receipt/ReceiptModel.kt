package com.wanggaspa.receipt

data class ReceiptItem(val name: String, val desc: String, val price: Long)
data class StoreInfo(val name: String, val tagline: String, val address: String, val contact: String)
data class Receipt(
    val store: StoreInfo,
    val customer: String,
    val customerPhone: String,
    val promoCode: String,
    val date: String,
    val time: String,
    val items: List<ReceiptItem>,
    val subtotal: Long,
    val total: Long,
    val paymentStatus: String,
    val footerLines: List<String>
)

fun sampleReceipt() = Receipt(
    StoreInfo("WANGGA SPA", "Sehat * Relaks * Bahagia",
        "Kayu Putih II No.32, Pulo Gadung, Jaktim",
        "Telp: 08211347294 | IG: @wangggasbabymomwoman"),
    "Mama Isaac", "08131006650", "", "28/09/2026", "14:15 WIB",
    listOf(
        ReceiptItem("Massage Kids", "Durasi 60 menit", 135000),
        ReceiptItem("Inflaren", "Durasi 30 menit", 50000),
        ReceiptItem("Transport PP (HM Care)", "Jarak & antar jemput", 15000)
    ),
    200000, 200000, "LUNAS Qris",
    listOf(
        "TERIMA KASIH ATAS KUNJUNGAN ANDA",
        "Kesehatan & Kebugaran Prioritas",
        "***Wangga Baby Mom Woman Spa***"
    )
)
