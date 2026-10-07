using System.Windows;
using WanggaSpa.Receipt;
using ReceiptModel = WanggaSpa.Receipt.Receipt;

namespace WanggaSpa.App;

public partial class MainWindow : Window
{
    readonly BluetoothPrinterService _printer = new();
    ReceiptModel? _current;

    public MainWindow()
    {
        InitializeComponent();
        OnRefreshPorts(null, null);
        OnPreview(null, null);
    }

    void OnRefreshPorts(object? s, RoutedEventArgs? e)
    {
        PortBox.ItemsSource = BluetoothPrinterService.ListPorts();
        if (PortBox.Items.Count > 0) PortBox.SelectedIndex = 0;
        else StatusText.Text = "COM port tidak ditemukan. Pair EXP583 V2 dulu di Settings > Bluetooth.";
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
            CustomerBox.Text.Trim(), PromoBox.Text.Trim(),
            now.ToString("dd/MM/yyyy"), now.ToString("HH:mm") + " WIB",
            items, total, total,
            ((System.Windows.Controls.ComboBoxItem)PayBox.SelectedItem).Content.ToString()!,
            new List<string> {
                "TERIMA KASIH ATAS KUNJUNGAN ANDA",
                "Kesehatan & Kebugaran Prioritas Kami",
                "*** Wangga Baby Mom Woman Spa ***" });
    }

    void OnPreview(object? s, RoutedEventArgs? e)
    {
        try { _current = BuildReceipt(); PreviewText.Text = ReceiptTextFormatter.BuildPreview(_current); }
        catch (Exception ex) { StatusText.Text = "Error: " + ex.Message; }
    }

    void OnTestPrint(object? s, RoutedEventArgs? e)
    {
        if (PortBox.SelectedItem is null) { StatusText.Text = "Pilih COM port dulu."; return; }
        try
        {
            _printer.Print(PortBox.SelectedItem.ToString()!, EscPosBuilder.Build(ReceiptTextFormatter.Sample()));
            StatusText.Text = "Test print terkirim.";
        }
        catch (Exception ex) { StatusText.Text = "Gagal print: " + ex.Message; }
    }

    void OnPrint(object? s, RoutedEventArgs? e)
    {
        if (PortBox.SelectedItem is null) { StatusText.Text = "Pilih COM port dulu."; return; }
        try
        {
            OnPreview(null, null);
            _printer.Print(PortBox.SelectedItem.ToString()!, EscPosBuilder.Build(_current!));
            StatusText.Text = $"Tercetak + tersimpan. Total {ReceiptTextFormatter.Rupiah(_current!.Total)}.";
        }
        catch (Exception ex) { StatusText.Text = "Gagal print: " + ex.Message; }
    }
}
