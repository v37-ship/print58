# WANGGA SPA — Receipt Printer (EXP583 V2, 58mm Bluetooth)

Native separate apps producing identical output on Windows + Android.

```
print58/
  shared-spec/        Single source of truth (layout, ESC/POS, sample JSON)
  windows/            C# .NET 8 WPF — Bluetooth SPP via virtual COM port
  android/            Kotlin + Jetpack Compose — Bluetooth Classic RFCOMM
```

## Printer

- Model: EXP583 V2 (often listed as EPX583 V2) — 58mm thermal, Bluetooth Classic SPP
- Paper: 58mm, printable 32 cols (Font A) / 42 cols (Font B). This project uses **32 cols, Font A**.
- ESC/POS, encoding Latin-1 / ASCII (Indonesian text, no CJK needed)
- Cut: `GS V 1`

## Quick start

### Windows (build on Windows)

```powershell
cd windows
dotnet build WanggaSpa.sln -c Release
dotnet run --project src/WanggaSpa.App
# 1. Pair EXP583 V2 in Windows Settings > Bluetooth
# 2. Check Device Manager > Ports (COM & LPT) for outgoing COM, e.g. COM5
# 3. In app: Bluetooth > select COM5 > Test Print > Print
```

### Android (build with Android Studio)

```
Open android/ in Android Studio > Run on device
1. Pair EXP583 in Android Bluetooth settings
2. In app: grant Bluetooth Connect/Scan > Bluetooth > tap EXP583 > Test Print
```

## Receipt layout (must match on both platforms)

See `shared-spec/receipt-spec.md` and `shared-spec/sample-transaction.json`.

## Data

Both apps use local SQLite, same schema (`transactions`, `services`, `settings`).
CSV export for daily reports. No server required.

## License

Internal use — WANGGA SPA.
