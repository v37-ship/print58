"""Python port of the 32-col formatter. Must match shared-spec/golden/*.txt byte-for-byte,
which is the same fixture the C# smoke tool and the Kotlin unit tests compile against.
Run: python3 tools/verify_receipt.py  (exit 0 = match)"""
import json, pathlib, sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
GOLDEN = ROOT / "shared-spec" / "golden"
WIDTH = 32


def printer_safe(s): return s.replace("•", "*").replace("–", "-").replace("—", "-")


def rupiah(v):
    return "Rp " + f"{v:,}".replace(",", ".")


def wrap(s, w=WIDTH):
    res = []
    for para in printer_safe(s).split("\n"):
        words = para.split()
        if not words:
            res.append("")
            continue
        cur = ""
        for word in words:
            if len(word) > w:
                if cur.strip():
                    res.append(cur.strip())
                    cur = ""
                for i in range(0, len(word), w):
                    res.append(word[i:i + w])
            elif len((cur + " " + word).strip()) > w:
                res.append(cur.strip())
                cur = word
            else:
                cur += " " + word
        if cur.strip():
            res.append(cur.strip())
    return res


def center(s, w=WIDTH):
    return "\n".join(" " * max(0, (w - len(l)) // 2) + l for l in wrap(s, w))


def two_col(left, right):
    l, r = printer_safe(left), printer_safe(right)
    if len(l) + 1 + len(r) <= WIDTH:
        return l + " " * (WIDTH - len(l) - len(r)) + r
    words, lines, cur = l.split(" "), [], ""
    for w in words:
        if len((cur + " " + w).strip()) > WIDTH:
            lines.append(cur.strip())
            cur = w
        else:
            cur += " " + w
    lines.append(cur.strip())
    out = lines[:-1]
    last = lines[-1]
    if len(last) + 1 + len(r) <= WIDTH:
        out.append(last + " " * (WIDTH - len(last) - len(r)) + r)
    else:
        out += [last, r.rjust(WIDTH)]
    return "\n".join(out)


def phone_or_dash(p): return "-" if not p.strip() else p.strip()


def discount_amount(subtotal, dtype, dval):
    if dtype == "%":
        return min(max(subtotal * min(max(dval, 0), 100) // 100, 0), subtotal)
    if dtype == "Rp":
        return min(max(dval, 0), subtotal)
    return 0


def build_preview(t):
    b = [center(t["store"]["name"]), center(t["store"]["tagline"]),
         center(t["store"]["address"]), center(t["store"]["contact"]),
         "-" * WIDTH,
         two_col(f"Pelanggan : {t['customer']}", t["date"]),
         two_col(f"Nomor HP  : {phone_or_dash(t.get('customer_phone', ''))}", t["time"])]
    if t.get("promo_code", "").strip():
        b.append(two_col(f"Promo     : {t['promo_code'].strip()}", ""))
    b += ["-" * WIDTH, two_col("Layanan / Produk", "Total"), "-" * WIDTH]
    for it in t["items"]:
        b.append(two_col(it["name"], rupiah(it["price"])))
        if it["desc"].strip():
            b += wrap(it["desc"])
    b += ["-" * WIDTH, two_col("Subtotal", rupiah(t["subtotal"]))]
    disc = discount_amount(t["subtotal"], t.get("discount_type", ""), t.get("discount_value", 0))
    if disc > 0:
        b.append(two_col("Diskon", "-" + rupiah(disc)) if t.get("discount_type") != "%"
                 else two_col(f"Diskon {min(max(t.get('discount_value', 0), 0), 100)}%", "-" + rupiah(disc)))
    b += ["-" * WIDTH, two_col("TOTAL AKHIR", rupiah(t["subtotal"] - disc)),
          two_col("Status Pembayaran", t["payment_status"]),
          "-" * WIDTH]
    for f in t["footer"]:
        b.append(center(f))
    return "\n".join(b)


CASES = {
    "promo-none": lambda t: t,
    "promo-filled": lambda t: dict(t, promo_code="WELCOME10"),
    "discount-10pct": lambda t: dict(t, discount_type="%", discount_value=10),
    "discount-nominal": lambda t: dict(t, discount_type="Rp", discount_value=25000),
    "discount-clamped": lambda t: dict(t, discount_type="%", discount_value=150),
    "empty-phone": lambda t: dict(t, customer_phone=" "),
}

t = json.loads((ROOT / "shared-spec" / "sample-transaction.json").read_text())
fail = 0
for name, mutate in CASES.items():
    path = GOLDEN / f"{name}.txt"
    if not path.exists():
        print(f"MISSING {name}.txt — run tools/dotnet-smoke --write")
        sys.exit(2)
    expected_raw = path.read_text()
    expected_lines = expected_raw.split("\n")
    expected_body = "\n".join(expected_lines[1:]).rstrip("\n")
    actual = build_preview(mutate(dict(t)))
    if expected_body == actual:
        print(f"OK   {name}")
    else:
        fail += 1
        print(f"FAIL {name}")
        exp, act = expected_body.split("\n"), actual.split("\n")
        for i in range(max(len(exp), len(act))):
            e = exp[i] if i < len(exp) else "<none>"
            a = act[i] if i < len(act) else "<none>"
            if e != a:
                print(f"  line {i+1} golden={e!r}")
                print(f"  line {i+1} py   ={a!r}")
print("---")
print(f"{len(CASES) - fail}/{len(CASES)} fixtures match")
sys.exit(1 if fail else 0)
