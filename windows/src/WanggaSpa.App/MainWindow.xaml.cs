using System.IO;
using System.Windows;
using WanggaSpa.Receipt;
using ReceiptModel = WanggaSpa.Receipt.Receipt;

namespace WanggaSpa.App;

public partial class MainWindow : Window
{
    readonly WindowsPrinterService _printer = new();
    static readonly string DbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WanggaSpa", "wangga.db");
    readonly Db _db = new(DbPath);
    ReceiptModel? _current;

    public MainWindow()
    {
        InitializeComponent();
        VersionStatus.Text = $"v{AppInfo.Version}";
        PreviewKeyDown += OnKey;
        OnRefreshPrinters(null, null);
        OnPreview(null, null);
        OnRefreshHistory(null, null);
    }

    void OnKey(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.System &&
            (e.SystemKey == System.Windows.Input.Key.LeftAlt || e.SystemKey == System.Windows.Input.Key.RightAlt))
        {
            if (!e.IsRepeat)
                MainMenu.Visibility = MainMenu.Visibility == Visibility.Visible
                    ? Visibility.Collapsed : Visibility.Visible;
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Escape && MainMenu.Visibility == Visibility.Visible)
        {
            MainMenu.Visibility = Visibility.Collapsed;
            e.Handled = true;
        }
    }

    void OnPrinterChanged(object? s, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (PrinterBox.SelectedItem is not null)
            PrinterStatus.Text = "Printer: " + PrinterBox.SelectedItem;
    }

    void OnRefreshPrinters(object? s, RoutedEventArgs? e)
    {
        PrinterBox.ItemsSource = WindowsPrinterService.ListPrinters();
        if (PrinterBox.Items.Count == 0)
            StatusText.Text = "Printer tidak ditemukan. Install driver thermal dulu di Settings > Bluetooth & devices > Printers.";
    }

    ReceiptModel BuildReceipt()
    {
        var now = DateTime.Now;
        var items = new List<ReceiptItem>();
        foreach (var line in ItemsBox.Text.Split('\n'))
        {
            var p = line.Split('|');
            if (p.Length < 3) continue;
            items.Add(new ReceiptItem(p[0].Trim(), p[1].Trim(), long.Parse(p[2].Trim())));
        }
        long total = items.Sum(i => i.Price);
        long.TryParse(DiscountBox.Text.Trim(), out long discVal);
        string discType = string.IsNullOrWhiteSpace(DiscountBox.Text) ? ""
            : ((System.Windows.Controls.ComboBoxItem)DiscountTypeBox.SelectedItem).Content.ToString()!;
        return new ReceiptModel(
            new StoreInfo("WANGGA SPA", "Sehat • Relaks • Bahagia",
                "Kayu Putih II No.32, Pulo Gadung, Jaktim",
                "Telp: 08211347294 | IG: @wangggasbabymomwoman"),
            CustomerBox.Text.Trim(), CustomerPhoneBox.Text.Trim(), PromoBox.Text.Trim(),
            discType, discVal,
            now.ToString("dd/MM/yyyy"), now.ToString("HH:mm") + " WIB",
            items, total,
            ((System.Windows.Controls.ComboBoxItem)PayBox.SelectedItem).Content.ToString()!,
            new List<string> {
                "TERIMA KASIH ATAS KUNJUNGAN ANDA",
                "Kesehatan & Kebugaran Prioritas",
                "***Wangga Baby Mom Woman Spa***" });
    }

    void OnPreview(object? s, RoutedEventArgs? e)
    {
        try { _current = BuildReceipt(); PreviewText.Text = ReceiptTextFormatter.BuildPreview(_current); }
        catch (Exception ex) { StatusText.Text = "Error: " + ex.Message; }
    }

    void OnTestPrint(object? s, RoutedEventArgs? e)
    {
        if (PrinterBox.SelectedItem is null) { StatusText.Text = "Pilih printer dulu."; return; }
        try
        {
            var test = BuildReceipt();
            PreviewText.Text = ReceiptTextFormatter.BuildPreview(test);
            _printer.Print(PrinterBox.SelectedItem.ToString()!, EscPosBuilder.Build(test));
            StatusText.Text = "Test print terkirim (isi sesuai form).";
        }
        catch (Exception ex) { StatusText.Text = "Gagal print: " + ex.Message; }
    }

    void OnPrint(object? s, RoutedEventArgs? e)
    {
        if (PrinterBox.SelectedItem is null) { StatusText.Text = "Pilih printer dulu."; return; }
        try
        {
            OnPreview(null, null);
            _printer.Print(PrinterBox.SelectedItem.ToString()!, EscPosBuilder.Build(_current!));
            _db.InsertTx(new Db.TxRow(0, _current!.Date, _current!.Time, _current!.Customer,
                _current!.CustomerPhone, _current!.PromoCode, _current!.DiscountType, _current!.DiscountValue,
                Db.ItemsJson(_current!.Items), _current!.Subtotal, _current!.GrandTotal, _current!.PaymentStatus));
            OnRefreshHistory(null, null);
            StatusText.Text = $"Tercetak + tersimpan. Total {ReceiptTextFormatter.Rupiah(_current!.GrandTotal)}.";
        }
        catch (Exception ex) { StatusText.Text = "Gagal print: " + ex.Message; }
    }

    void OnRefreshHistory(object? s, RoutedEventArgs? e)
    {
        try
        {
            var rows = _db.AllTx();
            HistoryGrid.ItemsSource = rows;
            var today = DateTime.Now.ToString("dd/MM/yyyy");
            DailyTotalText.Text = $"Hari ini ({today}): {ReceiptTextFormatter.Rupiah(_db.DailyTotal(today))} — {rows.Count} transaksi";
        }
        catch (Exception ex) { DailyTotalText.Text = "Gagal baca riwayat: " + ex.Message; }
    }

    void OnExportCsv(object? s, RoutedEventArgs? e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { FileName = "riwayat.csv", Filter = "CSV|*.csv" };
        if (dlg.ShowDialog() == true)
        {
            File.WriteAllText(dlg.FileName, Db.ToCsv(_db.AllTx(100000)));
            DailyTotalText.Text = "CSV tersimpan: " + dlg.FileName;
        }
    }

    string AboutDetail()
    {
        int txCount = 0;
        try { txCount = _db.AllTx(100000).Count; } catch { }
        return string.Join("\n", new[] {
            $"Aplikasi : WanggaSpa Kasir 58mm",
            $"Runtime  : {AppInfo.Runtime}",
            $"OS       : {Environment.OSVersion}",
            $"Printer  : {(PrinterBox.SelectedItem?.ToString() ?? "-")}",
            $"Kertas   : 58mm, 32 kolom, Font A",
            $"Database : {DbPath}",
            $"Transaksi: {txCount} tersimpan",
        });
    }

    void OnAbout(object? s, RoutedEventArgs? e) =>
        new AboutWindow(AboutDetail()) { Owner = this }.ShowDialog();

    void OnGuide(object? s, RoutedEventArgs? e) => MessageBox.Show(
        "CARA PAKAI\n\n" +
        "1. Install driver printer thermal (mis. POS-58).\n" +
        "2. Pilih printer di dropdown, klik Refresh bila kosong.\n" +
        "3. Klik Test Print untuk struk contoh sesuai form.\n" +
        "4. Isi Pelanggan, Nomor HP, Promo (boleh kosong), layanan & pembayaran.\n" +
        "5. Klik Print + Simpan. Otomatis tersimpan di tab Riwayat.\n" +
        "6. Tekan Alt untuk tampil/sembunyi menu Bantuan.",
        "Panduan Pakai", MessageBoxButton.OK, MessageBoxImage.Information);

    void OnBackupDb(object? s, RoutedEventArgs? e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
            { FileName = $"wangga-{DateTime.Now:yyyyMMdd}.db", Filter = "SQLite DB|*.db" };
        if (dlg.ShowDialog() == true)
        {
            File.Copy(DbPath, dlg.FileName, overwrite: true);
            MessageBox.Show("Backup tersimpan: " + dlg.FileName, "Backup Database",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    void OnCheckUpdate(object? s, RoutedEventArgs? e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                AppInfo.ReleasesUrl) { UseShellExecute = true });
        }
        catch (Exception ex) { MessageBox.Show("Gagal buka browser: " + ex.Message); }
    }

    void OnCopyDiagnostics(object? s, RoutedEventArgs? e)
    {
        try
        {
            var info = $"WanggaSpa v{AppInfo.Version} (build {AppInfo.BuildTime})\n" +
                AboutDetail() + "\nStatus: " + StatusText.Text;
            Clipboard.SetText(info);
            StatusText.Text = "Info diagnostik disalin ke clipboard.";
        }
        catch (Exception ex) { StatusText.Text = "Gagal salin: " + ex.Message; }
    }
}
