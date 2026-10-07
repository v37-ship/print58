# Android app — WanggaSpa (Kotlin + Jetpack Compose)

## Prereqs

- Android Studio Hedgehog+, JDK 17, Android SDK 34
- Pair EXP583 V2 in Android Settings > Bluetooth first.

## Build

```
Open android/ in Android Studio > Sync > Run on physical device
```

Bluetooth Classic needs runtime permissions: `BLUETOOTH_SCAN`, `BLUETOOTH_CONNECT`
(Android 12+) or `BLUETOOTH`/`ACCESS_FINE_LOCATION` on older versions.
Grant "Nearby devices" when prompted.

## Use

Kasir > Preview 32-kolom > Bluetooth (pilih EXP583 dari bonded devices) >
Test Print > Print + Simpan. Riwayat tersimpan di Room (SQLite), export CSV.

Formatting (`ReceiptTextFormatter.kt`, `EscPosBuilder.kt`) is byte-identical
to the Windows C# version — see `shared-spec/receipt-spec.md`.
