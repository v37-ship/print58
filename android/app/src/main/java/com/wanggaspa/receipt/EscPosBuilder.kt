package com.wanggaspa.receipt

import java.io.ByteArrayOutputStream

/** ESC/POS bytes for EXP583 V2 (58mm). Latin-1 encoding. */
object EscPosBuilder {
    private fun bytes(out: ByteArrayOutputStream, vararg b: Int) {
        for (x in b) out.write(x)
    }
    private fun text(out: ByteArrayOutputStream, s: String) {
        out.write(ReceiptTextFormatter.printerSafe(s).toByteArray(charset("ISO-8859-1")))
    }
    private fun line(out: ByteArrayOutputStream, s: String) { text(out, "$s\n") }

    fun build(r: Receipt): ByteArray {
        val b = ByteArrayOutputStream()
        bytes(b, 0x1B, 0x40)          // INIT
        bytes(b, 0x1B, 0x61, 0x01)    // CENTER
        bytes(b, 0x1D, 0x21, 0x11); bytes(b, 0x1B, 0x45, 0x01)
        line(b, ReceiptTextFormatter.printerSafe(r.store.name))
        bytes(b, 0x1D, 0x21, 0x00)
        line(b, ReceiptTextFormatter.printerSafe(r.store.tagline))
        bytes(b, 0x1B, 0x45, 0x00)
        ReceiptTextFormatter.wrap(r.store.address).forEach { line(b, it) }
        ReceiptTextFormatter.wrap(r.store.contact).forEach { line(b, it) }

        bytes(b, 0x1B, 0x61, 0x00)    // LEFT
        line(b, "-".repeat(32))
        ReceiptTextFormatter.twoCol("Pelanggan : ${r.customer}", r.date).split("\n").forEach { line(b, it) }
        ReceiptTextFormatter.twoCol("Nomor HP  : ${ReceiptTextFormatter.phoneOrDash(r.customerPhone)}", r.time).split("\n").forEach { line(b, it) }
        if (r.promoCode.isNotBlank())
            ReceiptTextFormatter.twoCol("Promo     : ${r.promoCode}", "").split("\n").forEach { line(b, it) }
        line(b, "-".repeat(32))
        bytes(b, 0x1B, 0x45, 0x01)
        ReceiptTextFormatter.twoCol("Layanan / Produk", "Total").split("\n").forEach { line(b, it) }
        bytes(b, 0x1B, 0x45, 0x00)
        line(b, "-".repeat(32))

        for (it in r.items) {
            bytes(b, 0x1B, 0x45, 0x01)
            ReceiptTextFormatter.twoCol(it.name, ReceiptTextFormatter.rupiah(it.price)).split("\n").forEach { l -> line(b, l) }
            bytes(b, 0x1B, 0x45, 0x00)
            ReceiptTextFormatter.wrap(it.desc).forEach { l -> line(b, l) }
            line(b, "")
        }

        line(b, "-".repeat(32))
        ReceiptTextFormatter.twoCol("Subtotal", ReceiptTextFormatter.rupiah(r.subtotal)).split("\n").forEach { line(b, it) }
        line(b, "-".repeat(32))
        bytes(b, 0x1B, 0x45, 0x01)
        // TOTAL AKHIR: bold, normal size (double-size overflows 32 cols).
        ReceiptTextFormatter.twoCol("TOTAL AKHIR", ReceiptTextFormatter.rupiah(r.total)).split("\n").forEach { line(b, it) }
        bytes(b, 0x1B, 0x45, 0x00)
        ReceiptTextFormatter.twoCol("Status Pembayaran", r.paymentStatus).split("\n").forEach { line(b, it) }
        line(b, "-".repeat(32))

        bytes(b, 0x1B, 0x61, 0x01); bytes(b, 0x1B, 0x45, 0x01)
        for (f in r.footerLines)
            ReceiptTextFormatter.center(f).split("\n").forEach { line(b, it) }
        bytes(b, 0x1B, 0x45, 0x00)
        bytes(b, 0x1B, 0x64, 0x04)
        bytes(b, 0x1D, 0x56, 0x01)
        return b.toByteArray()
    }
}
