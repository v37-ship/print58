"""Mirror of the C#/Kotlin 32-col formatter. Verifies layout + ESC/POS invariants."""
import json, pathlib

WIDTH = 32
FOOTER_WIDTH = 42

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
         two_col(f"Nomor HP  : {t['customer_phone']}", t["time"])]
    if t.get("promo_code", "").strip():
        b.append(two_col(f"Promo     : {t['promo_code'].strip()}", ""))
    b += ["-"*WIDTH, two_col("Layanan / Produk","Total"), "-"*WIDTH]
    for it in t["items"]:
        b.append(two_col(it["name"], rupiah(it["price"])))
        b += wrap(it["desc"]); b.append("")
    b += ["-"*WIDTH, two_col("Subtotal", rupiah(t["subtotal"])),
          "-"*WIDTH, two_col("TOTAL AKHIR", rupiah(t["total"])),
          two_col("Status Pembayaran", t["payment_status"]),
          "-"*WIDTH]
    for f in t["footer"]: b.append(center(f, FOOTER_WIDTH))
    return "\n".join(b)

t = json.loads(pathlib.Path("shared-spec/sample-transaction.json").read_text())
prev = build_preview(t)
print(prev)
print("="*WIDTH)
# assertions: body lines <=32, footer lines <=42, totals consistent
lines = prev.split("\n")
body, footer = lines[:-len(t["footer"])*1], None
# footer may wrap: find dash before footer
dashes = [i for i,l in enumerate(lines) if l == "-"*WIDTH]
fstart = dashes[-1] + 1
assert all(len(l) <= WIDTH for l in lines[:fstart]), "body line too long"
assert all(len(l) <= FOOTER_WIDTH for l in lines[fstart:]), "footer line too long"
# each original footer string fits on ONE condensed line
for f in t["footer"]:
    assert len(center(f, FOOTER_WIDTH).split("\n")) == 1, f"footer wraps: {f}"
assert t["subtotal"] == sum(i["price"] for i in t["items"]) == 200000
assert "WANGGA SPA" in prev and "TOTAL AKHIR" in prev and "Rp 200.000" in prev
assert "Mama Isaac" in prev and "LUNAS Qris" in prev and "WELCOME10" in prev
# empty promo -> no Promo line
t2 = dict(t, promo_code="  ")
no_promo_block = build_preview(t2).split("14:15 WIB")[1].split("-"*WIDTH)[0]
assert "Promo" not in no_promo_block, "empty promo must omit line"
print(f"OK: {len(lines)} lines, body_max={max(len(l) for l in lines[:fstart])}, footer_max={max(len(l) for l in lines[fstart:])}")
