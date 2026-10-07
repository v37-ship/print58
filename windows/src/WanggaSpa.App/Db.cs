using Microsoft.Data.Sqlite;

namespace WanggaSpa.App;

public sealed class Db
{
    readonly string _path;
    public Db(string path) { _path = path; Init(); }

    void Init()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        using var c = new SqliteConnection($"Data Source={_path}");
        c.Open();
        new SqliteCommand("""
            CREATE TABLE IF NOT EXISTS services(name TEXT PRIMARY KEY, desc TEXT NOT NULL, price INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS transactions(
              id INTEGER PRIMARY KEY AUTOINCREMENT, date TEXT NOT NULL, time TEXT NOT NULL,
              customer TEXT NOT NULL, promo_phone TEXT NOT NULL DEFAULT '',
              items_json TEXT NOT NULL, subtotal INTEGER NOT NULL, total INTEGER NOT NULL,
              payment_status TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS settings(k TEXT PRIMARY KEY, v TEXT NOT NULL);
            """, c).ExecuteNonQuery();
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
}
