namespace WanggaSpa.Receipt;

/// <summary>32-column text preview + shared formatting. Must stay identical to Android version.</summary>
public static class ReceiptTextFormatter
{
    public const int Width = 32;

    public static string Rupiah(long v) =>
        "Rp " + v.ToString("#,##0", new System.Globalization.CultureInfo("id-ID")).Replace(",", ".");

    public static string Dash() => new string('-', Width);

    public static string Center(string s)
    {
        var lines = Wrap(PrinterSafe(s), Width);
        return string.Join("\n", lines.Select(l =>
        {
            int pad = (Width - l.Length) / 2;
            return new string(' ', Math.Max(0, pad)) + l;
        }));
    }

    public static string TwoCol(string left, string right)
    {
        left = PrinterSafe(left); right = PrinterSafe(right);
        if (left.Length + 1 + right.Length <= Width)
            return left + new string(' ', Width - left.Length - right.Length) + right;
        // wrap left, keep right on last line
        var words = left.Split(' ');
        var lines = new List<string>(); var cur = "";
        foreach (var w in words)
        {
            if ((cur + " " + w).Trim().Length > Width) { lines.Add(cur.Trim()); cur = w; }
            else cur += " " + w;
        }
        lines.Add(cur.Trim());
        var out_ = new List<string>();
        for (int i = 0; i < lines.Count - 1; i++) out_.Add(lines[i]);
        var last = lines[^1];
        if (last.Length + 1 + right.Length <= Width)
            out_.Add(last + new string(' ', Width - last.Length - right.Length) + right);
        else { out_.Add(last); out_.Add(right.PadLeft(Width)); }
        return string.Join("\n", out_);
    }

    public static List<string> Wrap(string s, int w)
    {
        var res = new List<string>();
        foreach (var para in s.Split('\n'))
        {
            var words = para.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) { res.Add(""); continue; }
            var cur = "";
            foreach (var word in words)
            {
                if (word.Length > w) // hard-break long token
                {
                    if (cur.Length > 0) { res.Add(cur); cur = ""; }
                    for (int i = 0; i < word.Length; i += w) res.Add(word.Substring(i, Math.Min(w, word.Length - i)));
                }
                else if ((cur + " " + word).Trim().Length > w) { res.Add(cur.Trim()); cur = word; }
                else cur += " " + word;
            }
            if (cur.Trim().Length > 0) res.Add(cur.Trim());
        }
        return res;
    }

    public static string PrinterSafe(string s) =>
        (s ?? "").Replace("•", "*").Replace("–", "-").Replace("—", "-");

    public static string BuildPreview(Receipt r)
    {
        var b = new List<string>
        {
            Center(r.Store.Name),
            Center(r.Store.Tagline),
            Center(r.Store.Address),
            Center(r.Store.Contact),
            Dash(),
            TwoCol($"Pelanggan : {r.Customer}", r.Date),
            TwoCol($"Promo     : {r.PromoPhone}", r.Time),
            Dash(),
            TwoCol("Layanan / Produk", "Total"),
            Dash(),
        };
        foreach (var it in r.Items)
        {
            b.Add(TwoCol(it.Name, Rupiah(it.Price)));
            foreach (var d in Wrap(it.Desc, Width)) b.Add(d);
            b.Add("");
        }
        b.Add(Dash());
        b.Add(TwoCol("Subtotal", Rupiah(r.Subtotal)));
        b.Add(Dash());
        b.Add(TwoCol("TOTAL AKHIR", Rupiah(r.Total)));
        b.Add(TwoCol("Status Pembayaran", r.PaymentStatus));
        b.Add(Dash());
        foreach (var f in r.FooterLines) b.Add(Center(f));
        return string.Join("\n", b);
    }

    public static Receipt Sample() => new(
        new StoreInfo("WANGGA SPA", "Sehat * Relaks * Bahagia",
            "Kayu Putih II No.32, Pulo Gadung, Jaktim",
            "Telp: 08211347294 | IG: @wangggasbabymomwoman"),
        "Mama Isaac", "08131006650", "28/09/2026", "14:15 WIB",
        new List<ReceiptItem> {
            new("Massage Kids", "Durasi 60 menit", 135000),
            new("Inflaren", "Durasi 30 menit", 50000),
            new("Transport PP (HM Care)", "Jarak & antar jemput", 15000),
        },
        200000, 200000, "LUNAS Qris",
        new List<string> {
            "TERIMA KASIH ATAS KUNJUNGAN ANDA",
            "Kesehatan & Kebugaran Prioritas Kami",
            "*** Wangga Baby Mom Woman Spa ***" });
}
