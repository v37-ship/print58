# Receipt Spec — WANGGA SPA, 58mm, 32 columns

Target printer: EXP583 V2 Bluetooth (ESC/POS, 58mm). All text ASCII/Latin-1.
Reference image: `WANGGA SPA / Sehat • Relaks • Bahagia / Kayu Putih II No.32...`

## 1. Constants

- `WIDTH = 32`
- `DASH = "-" * 32`
- `SOLID = "=" * 32` — image uses a solid line under `Layanan / Produk | Total`; emulate with `--------------------------------` on printer or `-` bold. Spec uses `-` for dashed, and double-height `-` row is acceptable as `-` line. Implementations below use `-` for dashed and print the header-underline as `-` too (thermal has no true `=` distinction at small font; both look fine). Actually use `-` for dashed separators and print underline as 32x `-` in normal mode — matches image closely enough.
- Rupiah: `Rp 200.000` — prefix `Rp ` + thousands grouped by `.`, no decimals. `0 -> Rp 0`.
- Date: `DD/MM/YYYY` (e.g. `28/09/2026`), Time: `HH:MM WIB` (e.g. `14:15 WIB`).

## 2. Visual layout (32-char preview)

```
           WANGGA SPA            <- double-size, bold, centered
    Sehat • Relaks • Bahagia     <- bold, centered (• -> * on printer if needed)
Kayu Putih II No.32, Pulo Gadung,<- centered, wrap
             Jaktim              <- centered
Telp: 08211347294 | IG:          <- centered, wrap
    @wangggasbabymomwoman        <- centered
--------------------------------
Pelanggan : Mama Isaac 28/09/2026  <- special two-col, see §3
Nomor HP  : 08131006650 14:15 WIB  <- special two-col
Promo     : WELCOME10              <- only when promo code filled
--------------------------------
Layanan / Produk           Total
--------------------------------
Massage Kids           Rp 135.000
Durasi 60 menit
Inflaren               Rp 50.000
Durasi 30 menit
Transport PP (HM Care) Rp 15.000
Jarak & antar jemput
--------------------------------
Subtotal               Rp 200.000
--------------------------------
TOTAL AKHIR            Rp 200.000  <- bold, normal size
Status Pembayaran      LUNAS Qris
--------------------------------
  TERIMA KASIH ATAS KUNJUNGAN ANDA <- bold centered
 Kesehatan & Kebugaran Prioritas   <- centered
          Kami                     <- centered
*** Wangga Baby Mom Woman Spa ***  <- centered
```

## 3. Line rules

- `center(s)`: strip, wrap at 32, center each line.
- Customer block: two or three logical rows:
  - Row1 left = `Pelanggan : <name>`, right = `<date>`
  - Row2 left = `Nomor HP  : <phone>`, right = `<time>`
  - Row3 (only when promo code non-empty): `Promo     : <code>`, no right column.
  - Colons align: all labels are 11 chars before the value (`Pelanggan :`, `Nomor HP  :`, `Promo     :`).
  - Render as `left.padEnd(W - right.len) + right`. If left too long, wrap left first onto its own line then render last segment with right. Name truncated at 40 chars.
- Items: for each item:
  - Line1: `name` (bold) left, `Rp X` right via two-col. If name + price > 32, print name on its own line(s) wrapped, then price right-aligned on next line.
  - Line2+: `desc` wrapped, normal font, e.g. `Durasi 60 menit`.
- Totals: `Subtotal`, `TOTAL AKHIR` (bold, normal size — double-size overflows 32 cols), `Status Pembayaran` + status right.
- Feed 4 lines + cut at end.

## 4. ESC/POS bytes (identical both platforms)

```
INIT        1B 40
ALIGN_LEFT  1B 61 00
ALIGN_CTR   1B 61 01
ALIGN_RGT   1B 61 02
BOLD_ON     1B 45 01
BOLD_OFF    1B 45 00
SIZE_NORMAL 1D 21 00
SIZE_2X     1D 21 11   (double w+h, header store name only)
FONT_A      1B 4D 00   (normal, 32 cols)
FONT_B      1B 4D 01   (condensed, 42 cols — footer only)
FEED n      1B 64 n
CUT         1D 56 01
```

Sequence: INIT → header (CTR, 2X+BOLD store name, NORMAL+BOLD tagline, NORMAL address) → LEFT item/meta rows → CTR footer (Font B + BOLD, wrap/center at 42) → Font A → FEED 4 → CUT.

## 5. Edge cases

- Long service name (>20 chars with price): wrap name first.
- Price > Rp 99.999.999: right-align, allow left to wrap.
- Empty items: print `-` line + `TOTAL AKHIR Rp 0`.
- `•` (U+2022): printer Latin-1 has no `•`; replace with `*` or `-`. Implementations map `• -> *`.
- Bluetooth failure: show paired-device hint (Windows: check COM port; Android: re-pair + grant Nearby Devices permission).
