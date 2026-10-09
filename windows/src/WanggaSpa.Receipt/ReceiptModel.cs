namespace WanggaSpa.Receipt;

public sealed record ReceiptItem(string Name, string Desc, long Price);
public sealed record StoreInfo(string Name, string Tagline, string Address, string Contact);

public sealed record Receipt(
    StoreInfo Store,
    string Customer,
    string CustomerPhone,
    string PromoCode,
    string DiscountType,
    long DiscountValue,
    string Date,
    string Time,
    List<ReceiptItem> Items,
    long Subtotal,
    long Total,
    string PaymentStatus,
    List<string> FooterLines);
