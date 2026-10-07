using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public class InvoiceService : IInvoiceService
{
    // ---------------- Company Constants ----------------
    private const string CompanyName    = "VALOUS WORLD (OPC) PVT. LTD.";
    private const string CompanyGstin   = "22AALCV0369D1ZI";
    private const string CompanyPhone   = "8349442756";
    private const string CompanyEmail   = "valousworld@gmail.com";
    private const string CompanyAddress = "House No. 01, Street No. 2, Banjari Nagar, Near Sonkar Petrol Pump, Bhata Gaon, Raipur";
    private const string CompanyState   = "Chhattisgarh";
    private const string CompanyPin     = "492001";
    private const string CompanyWeb     = "www.valousworld.com";

    // Brand accent (near-black). Change here to re-skin the invoice.
    private const string AccentHex   = "#0A0A0A";
    private const string MutedHex    = "#6B6B6B";
    private const string SoftHex     = "#F5F5F5";
    private const string LineHex     = "#E5E5E5";
    private const string SuccessHex  = "#1B5E20";
    private const string DangerHex   = "#B71C1C";

    static InvoiceService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string GetInvoiceFileName(Order order)
        => $"ValousWorld-Invoice-{order.OrderNumber}.pdf";

    public byte[] GenerateInvoice(Order order)
    {
        var isCancelled = string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);
        var isCod       = string.Equals(order.PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase);
        var isPaid      = string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase);

        var invoiceNo   = $"INV-{order.OrderNumber}";
        var invoiceDate = order.PlacedAt.ToString("dd MMM yyyy");

        // GST split (India) — CGST+SGST for intra-state (Chhattisgarh = 22), else IGST.
        // NOTE: We treat Total as GST-INCLUSIVE. Breakout shown for compliance.
        const decimal gstRate = 0.18m;
        var gstTotal   = Math.Round(order.Total - (order.Total / (1 + gstRate)), 2);
        var taxable    = Math.Round(order.Total - gstTotal, 2);
        var isIntra    = string.Equals(order.ShippingState?.Trim(), CompanyState, StringComparison.OrdinalIgnoreCase);
        var cgst       = isIntra ? Math.Round(gstTotal / 2, 2) : 0m;
        var sgst       = isIntra ? Math.Round(gstTotal / 2, 2) : 0m;
        var igst       = isIntra ? 0m : gstTotal;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor("#111111"));
                page.PageColor(Colors.White);

                // ============================================================
                // HEADER — brand mark left, invoice meta right
                // ============================================================
                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        // Brand mark
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("VALOUSWORLD")
                                .FontSize(22).Bold().FontColor(AccentHex).LetterSpacing(0.15f);

                            c.Item().Text("Streetwear · Unisex · India")
                                .FontSize(9).FontColor(MutedHex).LetterSpacing(0.05f);
                        });

                        // Invoice meta
                        row.ConstantItem(210).Column(c =>
                        {
                            c.Item().AlignRight().Text(isCancelled ? "TAX INVOICE (CANCELLED)" : "TAX INVOICE")
                                .FontSize(12).Bold().FontColor(isCancelled ? DangerHex : AccentHex).LetterSpacing(0.1f);

                            c.Item().PaddingTop(2).AlignRight().Text($"Invoice No: {invoiceNo}")
                                .FontSize(9).FontColor(MutedHex);
                            c.Item().AlignRight().Text($"Invoice Date: {invoiceDate}")
                                .FontSize(9).FontColor(MutedHex);
                            c.Item().AlignRight().Text($"Order No: {order.OrderNumber}")
                                .FontSize(9).FontColor(MutedHex);
                        });
                    });

                    header.Item().PaddingTop(10).LineHorizontal(1.5f).LineColor(AccentHex);

                    // ========================================================
                    // COMPANY BLOCK
                    // ========================================================
                    header.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("BILLED BY").FontSize(8).Bold().FontColor(MutedHex).LetterSpacing(0.15f);
                            c.Item().PaddingTop(2).Text(CompanyName).FontSize(11).Bold().FontColor(AccentHex);
                            c.Item().Text($"GSTIN: {CompanyGstin}").FontSize(9).FontColor("#111111");
                            c.Item().Text(CompanyAddress).FontSize(9).FontColor("#111111");
                            c.Item().Text($"{CompanyState} — PIN {CompanyPin}").FontSize(9).FontColor("#111111");
                            c.Item().Text($"Contact: {CompanyPhone}  ·  Mail: {CompanyEmail}").FontSize(9).FontColor("#111111");
                            c.Item().Text($"Web: {CompanyWeb}").FontSize(9).FontColor("#111111");
                        });
                    });

                    // Cancelled ribbon
                    if (isCancelled)
                    {
                        header.Item().PaddingTop(10)
                            .Background("#FDECEA").Padding(8)
                            .AlignCenter()
                            .Text($"CANCELLED — {order.CancellationReason ?? "No reason provided"}")
                            .FontSize(10).Bold().FontColor(DangerHex).LetterSpacing(0.05f);
                    }

                    header.Item().PaddingTop(10).LineHorizontal(0.75f).LineColor(LineHex);
                });

                // ============================================================
                // CONTENT
                // ============================================================
                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(14);

                    // ---------- SHIP TO + ORDER META ----------
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("SHIP TO").FontSize(8).Bold().FontColor(MutedHex).LetterSpacing(0.15f);
                            c.Item().PaddingTop(3).Text(order.ShippingFullName).FontSize(11).Bold();
                            c.Item().Text($"{order.ShippingPhone}").FontSize(9.5f);
                            c.Item().Text(order.ShippingLine1).FontSize(9.5f);
                            if (!string.IsNullOrWhiteSpace(order.ShippingLine2))
                                c.Item().Text(order.ShippingLine2).FontSize(9.5f);
                            c.Item().Text($"{order.ShippingCity}, {order.ShippingState} — {order.ShippingPincode}").FontSize(9.5f);
                            c.Item().Text(order.ShippingCountry).FontSize(9.5f);
                        });

                        row.ConstantItem(220).Column(c =>
                        {
                            c.Item().Text("ORDER SUMMARY").FontSize(8).Bold().FontColor(MutedHex).LetterSpacing(0.15f);
                            c.Item().PaddingTop(3).Row(r =>
                            {
                                r.RelativeItem().Text("Order ID").FontSize(9.5f).FontColor(MutedHex);
                                r.RelativeItem().AlignRight().Text(order.OrderNumber).FontSize(9.5f).Bold();
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Placed On").FontSize(9.5f).FontColor(MutedHex);
                                r.RelativeItem().AlignRight().Text(order.PlacedAt.ToString("dd MMM yyyy, HH:mm")).FontSize(9.5f);
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Payment Method").FontSize(9.5f).FontColor(MutedHex);
                                r.RelativeItem().AlignRight().Text(isCod ? "Cash on Delivery" : "Prepaid (Razorpay)").FontSize(9.5f);
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Payment Status").FontSize(9.5f).FontColor(MutedHex);
                                r.RelativeItem().AlignRight().Text(order.PaymentStatus)
                                    .FontSize(9.5f).Bold()
                                    .FontColor(isPaid ? SuccessHex : (isCancelled ? DangerHex : "#111111"));
                            });
                        });
                    });

                    // ---------- ITEMS TABLE ----------
                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(24);   // #
                            columns.RelativeColumn(4);    // Item
                            columns.ConstantColumn(52);   // Size
                            columns.ConstantColumn(60);   // Color
                            columns.ConstantColumn(40);   // Qty
                            columns.ConstantColumn(72);   // Unit
                            columns.ConstantColumn(84);   // Total
                        });

                        // Header — soft gray, not heavy black
                        table.Header(h =>
                        {
                            void Head(IContainer cell, string text, bool right = false)
                            {
                                var c = cell.Background(SoftHex).Padding(7);
                                if (right) c = c.AlignRight();
                                c.Text(text).FontSize(9).Bold().FontColor(AccentHex).LetterSpacing(0.05f);
                            }

                            Head(h.Cell(), "#");
                            Head(h.Cell(), "ITEM");
                            Head(h.Cell(), "SIZE");
                            Head(h.Cell(), "COLOR");
                            Head(h.Cell(), "QTY", true);
                            Head(h.Cell(), "UNIT", true);
                            Head(h.Cell(), "TOTAL", true);
                        });

                        var index = 1;
                        foreach (var item in order.Items)
                        {
                            var bg = index % 2 == 0 ? "#FAFAFA" : "#FFFFFF";

                            void Cell(IContainer c, string text, bool bold = false, bool right = false)
                            {
                                var cc = c.Background(bg).BorderBottom(0.5f).BorderColor(LineHex).Padding(7);
                                if (right) cc = cc.AlignRight();
                                var t = cc.Text(text).FontSize(9.5f);
                                if (bold) t.Bold();
                            }

                            Cell(table.Cell(), index.ToString());
                            Cell(table.Cell(), item.ProductName, bold: true);
                            Cell(table.Cell(), item.Size ?? "—");
                            Cell(table.Cell(), item.Color ?? "—");
                            Cell(table.Cell(), item.Quantity.ToString(), right: true);
                            Cell(table.Cell(), $"₹{item.UnitPrice:0.00}", right: true);
                            Cell(table.Cell(), $"₹{item.LineTotal:0.00}", bold: true, right: true);

                            index++;
                        }
                    });

                    // ---------- TOTALS (right aligned block) ----------
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        // Left column: notes
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("NOTES").FontSize(8).Bold().FontColor(MutedHex).LetterSpacing(0.15f);
                            c.Item().PaddingTop(3).Text("Thank you for your order. All prices are inclusive of GST. Retain this invoice for warranty and returns within 7 days of delivery.")
                                .FontSize(9).FontColor(MutedHex).LineHeight(1.4f);
                        });

                        // Right column: totals
                        row.ConstantItem(240).Column(c =>
                        {
                          void Row(string label, string value, bool bold = false, string? color = null)
                            {
                                var textColor = color ?? "#111111";

                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(label).FontSize(9.5f).FontColor(bold ? AccentHex : MutedHex);
                                    var val = r.ConstantItem(110).AlignRight().Text(value)
                                        .FontSize(bold ? 10.5f : 9.5f)
                                        .FontColor(textColor);

                                    if (bold) val.Bold();
                                });
                            }

                            Row("Subtotal", $"₹{order.Subtotal:0.00}");
                            if (order.Discount > 0)
                                Row("Discount", $"−₹{order.Discount:0.00}", color: SuccessHex);
                            Row("Shipping", order.Shipping == 0 ? "FREE" : $"₹{order.Shipping:0.00}");
                            if (order.CodFee > 0)
                                Row("COD Handling", $"₹{order.CodFee:0.00}");

                            c.Item().PaddingTop(6).LineHorizontal(1).LineColor(AccentHex);
                            c.Item().PaddingTop(6).Row(r =>
                            {
                                r.RelativeItem().Text("TOTAL").FontSize(11).Bold().FontColor(AccentHex).LetterSpacing(0.1f);
                                r.ConstantItem(110).AlignRight().Text($"₹{order.Total:0.00}")
                                    .FontSize(13).Bold().FontColor(AccentHex);
                            });

                            c.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(LineHex);
                            c.Item().PaddingTop(6);

                            // GST breakout (inclusive)
                            Row("Taxable Value", $"₹{taxable:0.00}");
                            if (isIntra)
                            {
                                Row("CGST @ 9%", $"₹{cgst:0.00}");
                                Row("SGST @ 9%", $"₹{sgst:0.00}");
                            }
                            else
                            {
                                Row("IGST @ 18%", $"₹{igst:0.00}");
                            }
                            Row("Total GST (incl.)", $"₹{gstTotal:0.00}", bold: true);
                        });
                    });

                    // ---------- PAYMENT + SHIPPING SUMMARY ----------
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        // Payment box
                        row.RelativeItem().Background("#FAFAFA").Border(0.5f).BorderColor(LineHex).Padding(10).Column(c =>
                        {
                            c.Item().Text("PAYMENT DETAILS").FontSize(8).Bold().FontColor(MutedHex).LetterSpacing(0.15f);
                            c.Item().PaddingTop(3);

                            c.Item().Text($"Method: {(isCod ? "Cash on Delivery" : "Razorpay")}").FontSize(9.5f);
                            c.Item().Text($"Status: {order.PaymentStatus}").FontSize(9.5f);

                            if (!string.IsNullOrWhiteSpace(order.RazorpayPaymentId))
                                c.Item().Text($"Txn ID: {order.RazorpayPaymentId}").FontSize(9.5f);

                            if (isCod && !isCancelled)
                                c.Item().PaddingTop(3).Text($"Amount to collect: ₹{order.Total:0.00}")
                                    .FontSize(9.5f).Bold().FontColor(AccentHex);

                            if (!string.IsNullOrWhiteSpace(order.RefundId))
                                c.Item().PaddingTop(3).Text($"Refund: ₹{order.RefundedAmount:0.00} ({order.RefundStatus})")
                                    .FontSize(9.5f).FontColor(SuccessHex);
                        });

                        row.ConstantItem(12);

                        // Shipping box
                        row.RelativeItem().Background("#FAFAFA").Border(0.5f).BorderColor(LineHex).Padding(10).Column(c =>
                        {
                            c.Item().Text("SHIPPING DETAILS").FontSize(8).Bold().FontColor(MutedHex).LetterSpacing(0.15f);
                            c.Item().PaddingTop(3);

                            if (!string.IsNullOrWhiteSpace(order.TrackingNumber))
                            {
                                c.Item().Text($"Courier: {order.CourierName ?? "—"}").FontSize(9.5f);
                                c.Item().Text($"AWB / Tracking: {order.TrackingNumber}").FontSize(9.5f);
                                if (order.ShippedAt.HasValue)
                                    c.Item().Text($"Shipped: {order.ShippedAt.Value:dd MMM yyyy}").FontSize(9.5f);
                                if (order.DeliveredAt.HasValue)
                                    c.Item().Text($"Delivered: {order.DeliveredAt.Value:dd MMM yyyy}").FontSize(9.5f);
                            }
                            else
                            {
                                c.Item().Text("Shipment yet to be dispatched.").FontSize(9.5f).FontColor(MutedHex);
                            }

                            if (order.CodCollectedAt.HasValue)
                                c.Item().PaddingTop(3).Text($"COD collected on {order.CodCollectedAt.Value:dd MMM yyyy}")
                                    .FontSize(9.5f).FontColor(SuccessHex);
                        });
                    });

                    // ---------- CANCELLATION BLOCK ----------
                    if (isCancelled)
                    {
                        col.Item().PaddingTop(4).Background("#FDECEA").Padding(10).Column(c =>
                        {
                            c.Item().Text("CANCELLATION DETAILS").FontSize(8).Bold().FontColor(DangerHex).LetterSpacing(0.15f);
                            if (order.CancelledAt.HasValue)
                                c.Item().Text($"Cancelled on: {order.CancelledAt.Value:dd MMM yyyy, HH:mm}").FontSize(9.5f);
                            if (!string.IsNullOrWhiteSpace(order.CancelledBy))
                                c.Item().Text($"Cancelled by: {order.CancelledBy}").FontSize(9.5f);
                            if (!string.IsNullOrWhiteSpace(order.CancellationReason))
                                c.Item().Text($"Reason: {order.CancellationReason}").FontSize(9.5f);
                        });
                    }
                });

                // ============================================================
                // FOOTER
                // ============================================================
                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(0.75f).LineColor(LineHex);
                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Text($"© {DateTime.UtcNow.Year} {CompanyName}")
                            .FontSize(8).FontColor(MutedHex);

                        row.RelativeItem().AlignCenter().Text(
                            isCancelled
                                ? "This is a computer-generated cancellation record."
                                : "This is a computer-generated invoice. No signature required."
                        ).FontSize(8).FontColor(MutedHex);

                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Page ").FontSize(8).FontColor(MutedHex);
                            text.CurrentPageNumber().FontSize(8).FontColor(MutedHex);
                            text.Span(" of ").FontSize(8).FontColor(MutedHex);
                            text.TotalPages().FontSize(8).FontColor(MutedHex);
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}