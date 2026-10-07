# Windows app — WanggaSpa (C# .NET 8 WPF)

## Prereqs (on a Windows PC)

- .NET 8 SDK, Windows 10/11
- Pair EXP583 V2: Settings > Bluetooth & devices > Add device. Note outgoing COM port in Device Manager > Ports (COM & LPT), e.g. `COM5`.

## Build & run

```powershell
dotnet build WanggaSpa.sln -c Release
dotnet run --project src/WanggaSpa.App
```

## Use

1. Master: add services (name, desc, price).
2. Kasir: customer, promo phone, date/time (auto), pick services, payment status.
3. Preview: 32-col text preview (monospace) — must match `shared-spec/receipt-spec.md`.
4. Bluetooth: select COM port (default baud 9600 8N1 — baud is virtual for SPP), Test Print, Print + Cut.
5. Riwayat: history, daily total, CSV export.

## Projects

- `src/WanggaSpa.Receipt` — net8.0 class lib, no UI: `ReceiptModel.cs`, `ReceiptTextFormatter.cs` (32-col preview), `EscPosBuilder.cs` (bytes). Unit-testable on any OS.
- `src/WanggaSpa.App` — net8.0-windows WPF: `MainWindow`, `BluetoothPrinterService` (SerialPort), SQLite via `Microsoft.Data.Sqlite`.

## Troubleshooting EXP583 V2

- Not in COM list? Remove pairing, re-pair, check "Outgoing" COM in Bluetooth Settings > More options > COM Ports.
- Garbage chars? Keep encoding Latin-1 (`Encoding.GetEncoding(28591)`), don't send UTF-8 `•` — code maps it to `*`.
- No cut? Some firmware ignores `GS V`; tear manually — bytes still sent.
