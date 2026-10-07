package com.wanggaspa.receipt

import java.text.NumberFormat
import java.util.Locale

/** 32-column formatter. Must stay identical to Windows C# version. */
object ReceiptTextFormatter {
    const val WIDTH = 32

    fun phoneOrDash(p: String) = if (p.isBlank()) "-" else p.trim()
    fun dash() = "-".repeat(WIDTH)

    fun rupiah(v: Long): String {
        val nf = NumberFormat.getNumberInstance(Locale("id", "ID"))
        return "Rp " + nf.format(v).replace(',', '.').replace('\u00A0', '.')
    }

    fun printerSafe(s: String) = s.replace("•", "*").replace("–", "-").replace("—", "-")

    fun wrap(s: String, w: Int = WIDTH): List<String> {
        val res = mutableListOf<String>()
        for (para in printerSafe(s).split("\n")) {
            val words = para.split(" ").filter { it.isNotEmpty() }
            if (words.isEmpty()) { res.add(""); continue }
            var cur = ""
            for (word in words) {
                if (word.length > w) {
                    if (cur.isNotBlank()) { res.add(cur.trim()); cur = "" }
                    var i = 0
                    while (i < word.length) { res.add(word.substring(i, minOf(i + w, word.length))); i += w }
                } else if ((cur + " " + word).trim().length > w) { res.add(cur.trim()); cur = word }
                else cur += " $word"
            }
            if (cur.trim().isNotEmpty()) res.add(cur.trim())
        }
        return res
    }

    fun center(s: String): String = centerW(s, WIDTH)

    fun centerW(s: String, w: Int): String {
        val out = wrap(s, w).map { l ->
            val pad = (w - l.length) / 2
            " ".repeat(maxOf(0, pad)) + l
        }
        return out.joinToString("\n")
    }

    fun twoCol(left: String, right: String): String {
        val l = printerSafe(left); val r = printerSafe(right)
        if (l.length + 1 + r.length <= WIDTH)
            return l + " ".repeat(WIDTH - l.length - r.length) + r
        val words = l.split(" ")
        val lines = mutableListOf<String>()
        var cur = ""
        for (w in words) {
            if ((cur + " " + w).trim().length > WIDTH) { lines.add(cur.trim()); cur = w }
            else cur += " $w"
        }
        lines.add(cur.trim())
        val out = lines.dropLast(1).toMutableList()
        val last = lines.last()
        if (last.length + 1 + r.length <= WIDTH)
            out.add(last + " ".repeat(WIDTH - last.length - r.length) + r)
        else { out.add(last); out.add(r.padStart(WIDTH)) }
        return out.joinToString("\n")
    }

    fun buildPreview(r: Receipt): String {
        val b = mutableListOf<String>()
        b.add(center(r.store.name))
        b.add(center(r.store.tagline))
        b.add(center(r.store.address))
        b.add(center(r.store.contact))
        b.add(dash())
        b.add(twoCol("Pelanggan : ${r.customer}", r.date))
        b.add(twoCol("Nomor HP  : ${phoneOrDash(r.customerPhone)}", r.time))
        if (r.promoCode.isNotBlank())
            b.add(twoCol("Promo     : ${r.promoCode}", ""))
        b.add(dash())
        b.add(twoCol("Layanan / Produk", "Total"))
        b.add(dash())
        for (it in r.items) {
            b.add(twoCol(it.name, rupiah(it.price)))
            b.addAll(wrap(it.desc))
            b.add("")
        }
        b.add(dash())
        b.add(twoCol("Subtotal", rupiah(r.subtotal)))
        b.add(dash())
        b.add(twoCol("TOTAL AKHIR", rupiah(r.total)))
        b.add(twoCol("Status Pembayaran", r.paymentStatus))
        b.add(dash())
        for (f in r.footerLines) b.add(center(f))
        return b.joinToString("\n")
    }
}
