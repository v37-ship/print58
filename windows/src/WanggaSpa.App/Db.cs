using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace WanggaSpa.App;

public sealed class Db
{
    readonly string _path;
    public Db(string path) { _path = path; Init(); }

    static readonly string[] RetiredItems = new[]
    {
        "Massage Kids",
        "Inflaren",
        "Transport PP (HM Care)",
    };

    void SeedServices(SqliteConnection c)
    {
        foreach (var old in RetiredItems)
        {
            var del = new SqliteCommand("DELETE FROM services WHERE name=$n", c);
            del.Parameters.AddWithValue("$n", old);
            del.ExecuteNonQuery();
        }
        foreach (var (n, d, p) in new (string, string, long)[]
                 {
                   ("Baby Swim", "Durasi 20 menit", 75000),
                   ("Baby Massage", "Durasi 30 menit", 125000),
                   ("Terapi Infrared & Nebu", "Meringankan bapil baby, durasi 30 menit", 50000),
                   ("Kids Massage", "Kids massage + totok wajah, durasi 60 menit", 135000),
                   ("Paket Pijat Lengkap Baby SPA", "Durasi 60 menit", 150000),
                   ("Pijat Ibu Laktasi & Oksitosin", "Durasi 90 menit", 200000),
                   ("Pijat Ibu Hamil", "Durasi 90 menit", 200000),
                   ("Pijat Ibu Nifas", "Durasi 90 menit", 150000),
                   ("Baby SPA 0-12 bulan", "Durasi 30 menit", 65000),
                   ("Baby Treatment", "Baby massage + baby gym + berenang + mandi rempah, 60 menit", 90000),
                   ("Kid Treatment Totok", "Kids massage + totok wajah + maskeran, 50 menit", 100000),
                   ("Massage Kids Berkebutuhan Khusus", "Durasi 45 menit", 100000),
                   ("Kid Treatment Lulur", "Kids massage + lulur + maskeran + berenang, 90 menit", 150000),
                   ("Baby Swimming Set", "Berenang + mandi rempah + massage, 60 menit", 100000),
                   ("Baby SPA + Mom Treatment", "Baby massage + mandi rempah + mom massage + totok, 90 menit", 180000),
                   ("Paket Baby SPA 5x", "Baby massage + gym + berenang + mandi rempah, 1 bulan", 400000),
                   ("Kids Party Baby SPA + Mom", "90 menit, min. 3 anak", 150000),
                   ("Kids Party Massage Kids", "60 menit, min. 3 anak", 120000),
                   ("Kids Party Inflaren + Maskeran", "30 menit, min. 3 anak", 50000),
                   ("Full Body Massage Tradisional", "Free pedi + totok wajah, 90 menit", 150000),
                   ("Full Body Massage Balinesse", "Modern + stretching, free pedi + totok, 90 menit", 200000),
                   ("Tambahan Pijat", "Durasi 30 menit", 30000),
                   ("Lulur Buah Segar", "Timun/alpukat/buah naga, 20 menit", 40000),
                   ("Lulur Kopi / Cokelat", "Durasi 20 menit", 30000),
                   ("Sauna Box 30 mnt", "Durasi 30 menit", 75000),
                   ("Sauna Box 45 mnt", "Durasi 45 menit", 90000),
                   ("Reflexi + Full Body Massage", "Durasi 90 menit", 150000),
                   ("Totok Wajah Intensif", "Free pijat chest + kepala, 30 menit", 60000),
                   ("Foot Massage", "Durasi 30 menit", 50000),
                   ("Bekam + Warming", "Durasi 30 menit", 50000),
                   ("Kerokan", "Durasi 15 menit", 50000),
                   ("Cek IVA", "Deteksi ca mulut/rahim, 5 menit", 50000),
                   ("Papsmear", "", 500000),
                   ("Ratus", "Vagina bersih hygiene, 10 menit", 50000),
                   ("Cek Gula Darah", "Durasi 5 menit", 25000),
                   ("Cek Kolestrol", "Durasi 5 menit", 25000),
                   ("Cek Asam Urat", "Durasi 5 menit", 50000),
                   ("Transport PP 0-1 km", "Home care, PP", 5000),
                   ("Transport PP 2-3 km", "Home care, PP", 10000),
                   ("Transport PP 4-5 km", "Home care, PP", 20000),
                   ("Transport PP 6-7 km", "Home care, PP", 25000),
                   ("Transport PP 8-9 km", "Home care, PP", 30000),
                   ("Transport PP 10-12 km", "Home care, PP", 50000)
                 })
        {
            var cmd = new SqliteCommand("INSERT OR IGNORE INTO services(name,desc,price) VALUES($n,$d,$p)", c);
            cmd.Parameters.AddWithValue("$n", n); cmd.Parameters.AddWithValue("$d", d);
            cmd.Parameters.AddWithValue("$p", p);
            cmd.ExecuteNonQuery();
        }
    }

    void Init()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        using var c = new SqliteConnection($"Data Source={_path}");
        c.Open();
        new SqliteCommand("""
            CREATE TABLE IF NOT EXISTS services(name TEXT PRIMARY KEY, desc TEXT NOT NULL, price INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS transactions(
              id INTEGER PRIMARY KEY AUTOINCREMENT, date TEXT NOT NULL, time TEXT NOT NULL,
              customer TEXT NOT NULL, customer_phone TEXT NOT NULL DEFAULT '',
              promo_phone TEXT NOT NULL DEFAULT '',
              discount_type TEXT NOT NULL DEFAULT '', discount_value INTEGER NOT NULL DEFAULT 0,
              items_json TEXT NOT NULL, subtotal INTEGER NOT NULL, total INTEGER NOT NULL,
              payment_status TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS settings(k TEXT PRIMARY KEY, v TEXT NOT NULL);
            """, c).ExecuteNonQuery();
        // Migration for DBs created by older versions (promo_phone now holds the promo code).
        try { new SqliteCommand("ALTER TABLE transactions ADD COLUMN customer_phone TEXT NOT NULL DEFAULT ''", c).ExecuteNonQuery(); }
        catch (SqliteException) { /* column already exists */ }
        try { new SqliteCommand("ALTER TABLE transactions ADD COLUMN discount_type TEXT NOT NULL DEFAULT ''", c).ExecuteNonQuery(); }
        catch (SqliteException) { }
        try { new SqliteCommand("ALTER TABLE transactions ADD COLUMN discount_value INTEGER NOT NULL DEFAULT 0", c).ExecuteNonQuery(); }
        catch (SqliteException) { }
        SeedServices(c);
        foreach (var (k, v) in new Dictionary<string, string> {
            ["store_name"] = "WANGGA SPA",
            ["tagline"] = "Sehat • Relaks • Bahagia",
            ["address"] = "Kayu Putih II No.32, Pulo Gadung, Jaktim",
            ["contact"] = "Telp: 08211347294 | IG: @wangggasbabymomwoman",
        })
        {
            var cmd = new SqliteCommand("INSERT OR IGNORE INTO settings(k,v) VALUES($k,$v)", c);
            cmd.Parameters.AddWithValue("$k", k); cmd.Parameters.AddWithValue("$v", v);
            cmd.ExecuteNonQuery();
        }
    }

    public string Get(string k, string fb)
    {
        using var c = new SqliteConnection($"Data Source={_path}");
        c.Open();
        var cmd = new SqliteCommand("SELECT v FROM settings WHERE k=$k", c);
        cmd.Parameters.AddWithValue("$k", k);
        return cmd.ExecuteScalar()?.ToString() ?? fb;
    }

    public sealed record TxRow(long Id, string Date, string Time, string Customer,
        string CustomerPhone, string Promo, string DiscountType, long DiscountValue,
        string ItemsJson, long Subtotal, long Total, string Pay);

    public void InsertTx(TxRow t)
    {
        using var c = new SqliteConnection($"Data Source={_path}");
        c.Open();
        var cmd = new SqliteCommand(
            "INSERT INTO transactions(date,time,customer,customer_phone,promo_phone,discount_type,discount_value,items_json,subtotal,total,payment_status)" +
            " VALUES($d,$t,$cu,$cp,$pr,$dt,$dv,$ij,$st,$to,$pa)", c);
        cmd.Parameters.AddWithValue("$d", t.Date); cmd.Parameters.AddWithValue("$t", t.Time);
        cmd.Parameters.AddWithValue("$cu", t.Customer); cmd.Parameters.AddWithValue("$cp", t.CustomerPhone);
        cmd.Parameters.AddWithValue("$pr", t.Promo); cmd.Parameters.AddWithValue("$dt", t.DiscountType);
        cmd.Parameters.AddWithValue("$dv", t.DiscountValue); cmd.Parameters.AddWithValue("$ij", t.ItemsJson);
        cmd.Parameters.AddWithValue("$st", t.Subtotal); cmd.Parameters.AddWithValue("$to", t.Total);
        cmd.Parameters.AddWithValue("$pa", t.Pay);
        cmd.ExecuteNonQuery();
    }

    public List<TxRow> AllTx(int limit = 200)
    {
        var res = new List<TxRow>();
        using var c = new SqliteConnection($"Data Source={_path}");
        c.Open();
        var cmd = new SqliteCommand(
            "SELECT id,date,time,customer,customer_phone,promo_phone,discount_type,discount_value,items_json,subtotal,total,payment_status" +
            " FROM transactions ORDER BY id DESC LIMIT $n", c);
        cmd.Parameters.AddWithValue("$n", limit);
        using var rd = cmd.ExecuteReader();
        while (rd.Read()) res.Add(new TxRow(rd.GetInt64(0), rd.GetString(1), rd.GetString(2),
            rd.GetString(3), rd.GetString(4), rd.GetString(5), rd.GetString(6), rd.GetInt64(7),
            rd.GetString(8), rd.GetInt64(9), rd.GetInt64(10), rd.GetString(11)));
        return res;
    }

    public long DailyTotal(string date)
    {
        using var c = new SqliteConnection($"Data Source={_path}");
        c.Open();
        var cmd = new SqliteCommand("SELECT COALESCE(SUM(total),0) FROM transactions WHERE date=$d", c);
        cmd.Parameters.AddWithValue("$d", date);
        return (long)(cmd.ExecuteScalar() ?? 0L);
    }

    // ---- services master ----
    public sealed record SvcRow(string Name, string Desc, long Price);

    public List<SvcRow> AllServices()
    {
        var res = new List<SvcRow>();
        using var c = new SqliteConnection($"Data Source={_path}");
        c.Open();
        var cmd = new SqliteCommand("SELECT name,desc,price FROM services ORDER BY name", c);
        using var rd = cmd.ExecuteReader();
        while (rd.Read()) res.Add(new SvcRow(rd.GetString(0), rd.GetString(1), rd.GetInt64(2)));
        return res;
    }

    public void SaveService(string name, string desc, long price)
    {
        using var c = new SqliteConnection($"Data Source={_path}");
        c.Open();
        var cmd = new SqliteCommand(
            "INSERT INTO services(name,desc,price) VALUES($n,$d,$p) " +
            "ON CONFLICT(name) DO UPDATE SET desc=$d, price=$p", c);
        cmd.Parameters.AddWithValue("$n", name);
        cmd.Parameters.AddWithValue("$d", desc);
        cmd.Parameters.AddWithValue("$p", price);
        cmd.ExecuteNonQuery();
    }

    public void DeleteService(string name)
    {
        using var c = new SqliteConnection($"Data Source={_path}");
        c.Open();
        var cmd = new SqliteCommand("DELETE FROM services WHERE name=$n", c);
        cmd.Parameters.AddWithValue("$n", name);
        cmd.ExecuteNonQuery();
    }

    public static string ToCsv(IEnumerable<TxRow> rows)
    {
        var sb = new System.Text.StringBuilder("id,date,time,customer,phone,promo,discount_type,discount_value,items,subtotal,total,payment\n");
        foreach (var r in rows)
            sb.Append($"{r.Id},{r.Date},{r.Time},{Csv(r.Customer)},{Csv(r.CustomerPhone)},{Csv(r.Promo)},{r.DiscountType},{r.DiscountValue},{Csv(ItemsSummary(r.ItemsJson))},{r.Subtotal},{r.Total},{Csv(r.Pay)}\n");
        return sb.ToString();
        static string Csv(string s) => s.Contains(',') || s.Contains('"') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
    }

    public static string ItemsJson(List<Receipt.ReceiptItem> items) =>
        JsonSerializer.Serialize(items.Select(i => new { i.Name, i.Desc, i.Price, i.Qty }));

    /// <summary>"Massage Kids x2 @135000; ..." for CSV export.</summary>
    public static string ItemsSummary(string itemsJson)
    {
        try
        {
            var items = JsonSerializer.Deserialize<List<ReceiptJson>>(itemsJson) ?? new();
            return string.Join("; ", items.Select(i =>
                i.Qty > 1 ? $"{i.Name} x{i.Qty} @{i.Price}" : $"{i.Name} @{i.Price}"));
        }
        catch { return itemsJson ?? ""; }
    }

    private sealed record ReceiptJson(string Name, string Desc, long Price, int Qty);
}
