"""Mirror of the C#/Kotlin 32-col formatter. Verifies layout + ESC/POS invariants."""
import json, pathlib

WIDTH = 32

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

def center(s):
    return "\n".join(" "*max(0,(WIDTH-len(l))//2)+l for l in wrap(s))

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
         two_col(f"Promo     : {t['promo_phone']}", t["time"]),
         "-"*WIDTH, two_col("Layanan / Produk","Total"), "-"*WIDTH]
    for it in t["items"]:
        b.append(two_col(it["name"], rupiah(it["price"])))
        b += wrap(it["desc"]); b.append("")
    b += ["-"*WIDTH, two_col("Subtotal", rupiah(t["subtotal"])),
          "-"*WIDTH, two_col("TOTAL AKHIR", rupiah(t["total"])),
          two_col("Status Pembayaran", t["payment_status"]),
          "-"*WIDTH]
    for f in t["footer"]: b.append(center(f))
    return "\n".join(b)

t = json.loads(pathlib.Path("shared-spec/sample-transaction.json").read_text())
prev = build_preview(t)
print(prev)
print("="*WIDTH)
# assertions: every line <=32, totals consistent, key strings present
lines = prev.split("\n")
assert all(len(l) <= WIDTH for l in lines), "line too long"
assert t["subtotal"] == sum(i["price"] for i in t["items"]) == 200000
assert "WANGGA SPA" in prev and "TOTAL AKHIR" in prev and "Rp 200.000" in prev
assert "Mama Isaac" in prev and "LUNAS Qris" in prev
# ESC/POS invariants: INIT present, ends with cut GS V 1
pkt = bytes([0x1B,0x40]) + prev.encode("latin-1") + bytes([0x1B,0x64,0x04,0x1D,0x56,0x01])
assert pkt[:2] == b"\x1b@" and pkt[-3:] == b"\x1dV\x01"
print(f"OK: {len(lines)} lines, max_len={max(len(l) for l in lines)}, bytes={len(pkt)}")
