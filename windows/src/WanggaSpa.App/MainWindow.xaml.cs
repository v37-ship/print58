using System.IO;
using System.Windows;
using WanggaSpa.Receipt;
using ReceiptModel = WanggaSpa.Receipt.Receipt;

namespace WanggaSpa.App;

public partial class MainWindow : Window
{
    readonly WindowsPrinterService _printer = new();
    readonly Db _db = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WanggaSpa", "wangga.db"));
    ReceiptModel? _current;

    public MainWindow()
    {
        InitializeComponent();
        OnRefreshPrinters(null, null);
        OnPreview(null, null);
        OnRefreshHistory(null, null);
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
        return new ReceiptModel(
            new StoreInfo("WANGGA SPA", "Sehat • Relaks • Bahagia",
                "Kayu Putih II No.32, Pulo Gadung, Jaktim",
                "Telp: 08211347294 | IG: @wangggasbabymomwoman"),
            CustomerBox.Text.Trim(), CustomerPhoneBox.Text.Trim(), PromoBox.Text.Trim(),
            now.ToString("dd/MM/yyyy"), now.ToString("HH:mm") + " WIB",
            items, total, total,
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
            _printer.Print(PrinterBox.SelectedItem.ToString()!, EscPosBuilder.Build(ReceiptTextFormatter.Sample()));
            StatusText.Text = "Test print terkirim.";
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
                _current!.CustomerPhone, _current!.PromoCode,
                Db.ItemsJson(_current!.Items), _current!.Subtotal, _current!.Total, _current!.PaymentStatus));
            OnRefreshHistory(null, null);
            StatusText.Text = $"Tercetak + tersimpan. Total {ReceiptTextFormatter.Rupiah(_current!.Total)}.";
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
}
