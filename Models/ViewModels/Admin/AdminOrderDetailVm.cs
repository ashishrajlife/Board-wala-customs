using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.ViewModels.Admin;

public class AdminOrderDetailVm
{
    public Order Order { get; set; } = null!;
    public User? Customer { get; set; }
    public List<WebhookEvent> WebhookEvents { get; set; } = new();

    // Derived flags for the action panel
    public bool CanCancel =>
        Order.Status != OrderStatus.Cancelled &&
        Order.Status != OrderStatus.Delivered &&
        Order.Status != OrderStatus.Refunded &&
        Order.PaymentStatus != PaymentStatus.Refunded;

    public bool CanMarkDelivered =>
        Order.PaymentMethod == PaymentMethods.Cod &&
        Order.Status == OrderStatus.Shipped &&
        Order.PaymentStatus == PaymentStatus.CodPending;

    public bool CanRefund =>
        Order.PaymentMethod == PaymentMethods.Razorpay &&
        Order.PaymentStatus == PaymentStatus.Paid &&
        string.IsNullOrEmpty(Order.RefundId);

    public bool CanResync =>
        !string.IsNullOrEmpty(Order.TrackingNumber);

    public string DisplayEmail => Customer?.Email ?? "—";
    public string DisplayUserName => Customer?.FullName ?? $"User #{Order.UserId}";
}