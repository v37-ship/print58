"""Mirror of the C#/Kotlin 32-col formatter. Verifies layout + ESC/POS invariants."""
import json, pathlib

WIDTH = 32

def phone_or_dash(p): return "-" if not p.strip() else p.strip()

def discount_amount(subtotal, dtype, dval):
    if dtype == "%": return min(max(subtotal * min(max(dval, 0), 100) // 100, 0), subtotal)
    if dtype == "Rp": return min(max(dval, 0), subtotal)
    return 0

def discount_label(dtype, dval):
    return f"Diskon {min(max(dval, 0), 100)}%" if dtype == "%" else "Diskon"

def printer_safe(s): return s.replace("•","*").replace("–","-").replace("—","-")

def rupiah(v):
    return "Rp " + f"{v:,}".replace(",", ".")

def wrap(s, w=WIDTH):
    res = []
    for para in printer_safe(s).split("\n"):
        words = para.split()
        if not words: res.append(""); continue
        cur = ""
        for word in words:
            if len(word) > w:
                if cur.strip(): res.append(cur.strip()); cur = ""
                for i in range(0, len(word), w): res.append(word[i:i+w])
            elif len((cur+" "+word).strip()) > w:
                res.append(cur.strip()); cur = word
            else: cur += " " + word
        if cur.strip(): res.append(cur.strip())
    return res

def center(s, w=WIDTH):
    return "\n".join(" "*max(0,(w-len(l))//2)+l for l in wrap(s, w))

def two_col(left, right):
    l, r = printer_safe(left), printer_safe(right)
    if len(l)+1+len(r) <= WIDTH:
        return l + " "*(WIDTH-len(l)-len(r)) + r
    words, lines, cur = l.split(" "), [], ""
    for w in words:
        if len((cur+" "+w).strip()) > WIDTH: lines.append(cur.strip()); cur = w
        else: cur += " "+w
    lines.append(cur.strip())
    out = lines[:-1]; last = lines[-1]
    if len(last)+1+len(r) <= WIDTH: out.append(last+" "*(WIDTH-len(last)-len(r))+r)
    else: out += [last, r.rjust(WIDTH)]
    return "\n".join(out)

def build_preview(t):
    b = [center(t["store"]["name"]), center(t["store"]["tagline"]),
         center(t["store"]["address"]), center(t["store"]["contact"]),
         "-"*WIDTH,
         two_col(f"Pelanggan : {t['customer']}", t["date"]),
         two_col(f"Nomor HP  : {phone_or_dash(t.get('customer_phone',''))}", t["time"])]
    if t.get("promo_code", "").strip():
        b.append(two_col(f"Promo     : {t['promo_code'].strip()}", ""))
    b += ["-"*WIDTH, two_col("Layanan / Produk","Total"), "-"*WIDTH]
    for it in t["items"]:
        b.append(two_col(it["name"], rupiah(it["price"])))
        if it["desc"].strip(): b += wrap(it["desc"])
    b += ["-"*WIDTH, two_col("Subtotal", rupiah(t["subtotal"]))]
    disc = discount_amount(t["subtotal"], t.get("discount_type", ""), t.get("discount_value", 0))
    if disc > 0:
        b.append(two_col(discount_label(t["discount_type"], t["discount_value"]), "-" + rupiah(disc)))
    b += ["-"*WIDTH, two_col("TOTAL AKHIR", rupiah(t["subtotal"] - disc)),
          two_col("Status Pembayaran", t["payment_status"]),
          "-"*WIDTH]
    for f in t["footer"]: b.append(center(f))
    return "\n".join(b)

t = json.loads(pathlib.Path("shared-spec/sample-transaction.json").read_text())
prev = build_preview(t)
print(prev)
print("="*WIDTH)
# assertions: every line <=32, totals consistent
lines = prev.split("\n")
assert all(len(l) <= WIDTH for l in lines), [l for l in lines if len(l) > WIDTH]
assert t["subtotal"] == sum(i["price"] for i in t["items"]) == 200000
assert "WANGGA SPA" in prev and "TOTAL AKHIR" in prev and "Rp 200.000" in prev
assert "Mama Isaac" in prev and "LUNAS Qris" in prev
assert "Nomor HP  : 08131006650" in prev
# sample has empty promo -> no Promo line; with promo -> line appears
assert "Promo" not in prev.split("Nomor HP")[1].split("-"*WIDTH)[0], "empty promo must omit line"
t3 = dict(t, promo_code="WELCOME10")
assert "Promo     : WELCOME10" in build_preview(t3), "filled promo must print"
# empty promo -> no Promo line; empty phone -> dash
t2 = dict(t, promo_code="  ", customer_phone=" ")
p2 = build_preview(t2)
assert "Promo" not in p2.split("Nomor HP")[1].split("-"*WIDTH)[0], "empty promo must omit line"
assert "Nomor HP  : -" in p2, "empty phone must show dash"
# discount cases: 10% of 200.000 = 20.000 -> total 180.000
t4 = dict(t, discount_type="%", discount_value=10)
p4 = build_preview(t4)
assert "Diskon 10%" in p4 and "-Rp 20.000" in p4 and "Rp 180.000" in p4
# nominal 25.000 -> total 175.000
t5 = dict(t, discount_type="Rp", discount_value=25000)
p5 = build_preview(t5)
assert "Diskon" in p5 and "-Rp 25.000" in p5 and "Rp 175.000" in p5
# clamps: 150% -> 100%, 999.999.999 -> subtotal; total never negative
t6 = dict(t, discount_type="%", discount_value=150)
assert "Rp 0" in build_preview(t6).split("TOTAL AKHIR")[1]
t7 = dict(t, discount_type="Rp", discount_value=999999999)
assert "Rp 0" in build_preview(t7).split("TOTAL AKHIR")[1]
assert all(len(l) <= WIDTH for l in p4.split("\n") + p5.split("\n")), "discount lines too long"
print(f"OK: {len(lines)} lines, max_len={max(len(l) for l in lines)}")
