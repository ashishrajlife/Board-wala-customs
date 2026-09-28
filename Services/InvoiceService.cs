using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public class InvoiceService : IInvoiceService
{
    static InvoiceService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        // No UseSystemFonts — safer for production
        // Using built-in Lato font (bundled with QuestPDF)
    }

    public string GetInvoiceFileName(Order order)
        => $"VALOUSWORLD-Invoice-{order.OrderNumber}.pdf";

    public byte[] GenerateInvoice(Order order)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);

                // ✅ Lato — QuestPDF ka built-in font, har environment me kaam karega
                page.DefaultTextStyle(x => x.FontSize(10));

                // ---------- HEADER ----------
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("VALOUSWORLD").FontSize(22).Bold();
                            c.Item().Text("Streetwear · Unisex · India").FontSize(9).FontColor(Colors.Grey.Medium);
                        });

                        row.ConstantItem(180).Column(c =>
                        {
                            c.Item().AlignRight().Text("TAX INVOICE").FontSize(14).Bold();
                            c.Item().AlignRight().Text($"Invoice: {order.OrderNumber}").FontSize(9);
                            c.Item().AlignRight().Text($"Date: {order.PlacedAt:dd MMM yyyy}").FontSize(9);
                            c.Item().AlignRight().Text($"Status: {order.PaymentStatus}").FontSize(9);
                        });
                    });

                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Black);
                });

                // ---------- CONTENT ----------
                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Spacing(15);

                    // BILL TO / SHIP TO
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("BILL TO / SHIP TO").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                            c.Item().PaddingTop(4).Text(order.ShippingFullName).FontSize(11).Bold();
                            c.Item().Text(order.ShippingPhone).FontSize(10);
                            c.Item().Text(order.ShippingLine1).FontSize(10);
                            if (!string.IsNullOrWhiteSpace(order.ShippingLine2))
                                c.Item().Text(order.ShippingLine2).FontSize(10);
                            c.Item().Text($"{order.ShippingCity}, {order.ShippingState} — {order.ShippingPincode}").FontSize(10);
                            c.Item().Text(order.ShippingCountry).FontSize(10);
                        });
                    });

                    // ITEMS TABLE
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(90);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Black).Padding(6).Text("#").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(Colors.Black).Padding(6).Text("ITEM").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(Colors.Black).Padding(6).Text("SIZE").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(Colors.Black).Padding(6).Text("COLOR").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(Colors.Black).Padding(6).AlignRight().Text("QTY").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(Colors.Black).Padding(6).AlignRight().Text("UNIT").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(Colors.Black).Padding(6).AlignRight().Text("TOTAL").FontColor(Colors.White).FontSize(9).Bold();
                        });

                        var index = 1;
                        foreach (var item in order.Items)
                        {
                            var bg = index % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;

                            table.Cell().Background(bg).Padding(6).Text(index.ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(6).Text(item.ProductName).FontSize(9).Bold();
                            table.Cell().Background(bg).Padding(6).Text(item.Size ?? "—").FontSize(9);
                            table.Cell().Background(bg).Padding(6).Text(item.Color ?? "—").FontSize(9);
                            table.Cell().Background(bg).Padding(6).AlignRight().Text(item.Quantity.ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(6).AlignRight().Text($"₹{item.UnitPrice:0.00}").FontSize(9);
                            table.Cell().Background(bg).Padding(6).AlignRight().Text($"₹{item.LineTotal:0.00}").FontSize(9).Bold();

                            index++;
                        }
                    });

                    // TOTALS
                    col.Item().PaddingTop(10).AlignRight().Column(c =>
                    {
                        c.Spacing(4);
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(140).Text("Subtotal").FontSize(10);
                            r.ConstantItem(100).AlignRight().Text($"₹{order.Subtotal:0.00}").FontSize(10);
                        });
                        if (order.Discount > 0)
                        {
                            c.Item().Row(r =>
                            {
                                r.ConstantItem(140).Text("Discount").FontSize(10).FontColor(Colors.Green.Darken2);
                                r.ConstantItem(100).AlignRight().Text($"−₹{order.Discount:0.00}").FontSize(10).FontColor(Colors.Green.Darken2);
                            });
                        }
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(140).Text("Shipping").FontSize(10);
                            r.ConstantItem(100).AlignRight().Text(order.Shipping == 0 ? "FREE" : $"₹{order.Shipping:0.00}").FontSize(10);
                        });
                        c.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Black);
                        c.Item().PaddingTop(4).Row(r =>
                        {
                            r.ConstantItem(140).Text("TOTAL").FontSize(12).Bold();
                            r.ConstantItem(100).AlignRight().Text($"₹{order.Total:0.00}").FontSize(12).Bold();
                        });
                    });

                    // PAYMENT INFO
                    col.Item().PaddingTop(20).Background(Colors.Grey.Lighten4).Padding(12).Column(c =>
                    {
                        c.Item().Text("PAYMENT DETAILS").FontSize(9).Bold();
                        c.Item().PaddingTop(4).Text($"Method: {order.PaymentMethod}").FontSize(9);
                        c.Item().Text($"Status: {order.PaymentStatus}").FontSize(9);
                        if (!string.IsNullOrWhiteSpace(order.RazorpayPaymentId))
                            c.Item().Text($"Transaction ID: {order.RazorpayPaymentId}").FontSize(9);
                        if (!string.IsNullOrWhiteSpace(order.TrackingNumber))
                            c.Item().Text($"Tracking: {order.CourierName} — {order.TrackingNumber}").FontSize(9);
                    });
                });

                // FOOTER
                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    col.Item().PaddingTop(8).AlignCenter().Text(text =>
                    {
                        text.Span("Thank you for shopping with VALOUSWORLD! ").FontSize(9).FontColor(Colors.Grey.Darken2);
                        text.Span("· For queries: support@valousworld.com").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                    col.Item().AlignCenter().Text("This is a computer-generated invoice. No signature required.")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }
}