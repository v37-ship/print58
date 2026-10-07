# Windows app — WanggaSpa (C# .NET 8 WPF)

## Prereqs (on a Windows PC)

- Windows 10/11, no .NET install needed (self-contained exe)
- Thermal printer driver installed, e.g. queue `POS-58 (1)` visible in
  Settings > Bluetooth & devices > Printers & scanners.
  Check exact queue name with `Get-Printer` in PowerShell.

## Build & run

```powershell
dotnet build WanggaSpa.sln -c Release
dotnet run --project src/WanggaSpa.App
```

Single-file exe: `dotnet publish src/WanggaSpa.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/win-x64`
— copy the whole `publish/win-x64` folder (or just `WanggaSpa.exe`) to the PC and run.

## Use

1. Master: add services (name, desc, price).
2. Kasir: customer, promo phone, date/time (auto), pick services, payment status.
3. Preview: 32-col text preview (monospace) — must match `shared-spec/receipt-spec.md`.
4. Printer: select the queue (e.g. `POS-58 (1)`) > Test Print > Print + Cut.
   Bytes are sent RAW via the spooler (`winspool.drv WritePrinter`), so the
   driver's paper settings don't reformat the receipt.
5. Riwayat: history, daily total, CSV export.

## Projects

- `src/WanggaSpa.Receipt` — net8.0 class lib, no UI: `ReceiptModel.cs`, `ReceiptTextFormatter.cs` (32-col preview), `EscPosBuilder.cs` (bytes). Unit-testable on any OS.
- `src/WanggaSpa.App` — net8.0-windows WPF: `MainWindow`, `BluetoothPrinterService` (SerialPort), SQLite via `Microsoft.Data.Sqlite`.

## Troubleshooting POS-58

- Queue not listed? Reinstall the thermal driver, then verify with `Get-Printer` in PowerShell.
- Garbage chars? Keep encoding Latin-1 (`Encoding.GetEncoding(28591)`), don't send UTF-8 `•` — code maps it to `*`.
- Receipt too wide/narrow? Set paper size to 58mm in Printing Preferences; bytes are RAW so layout is fixed 32 cols.
- No cut? Some firmware ignores `GS V`; tear manually — bytes still sent.
