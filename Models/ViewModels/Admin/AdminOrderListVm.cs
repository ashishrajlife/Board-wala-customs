using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.ViewModels.Admin;

public class AdminOrderListVm
{
    public List<Order> Orders { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 1;

    // Filter echo
    public string? Status { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Search { get; set; }

    // Counters (unaffected by filters — always show global totals)
    public int CountAll { get; set; }
    public int CountConfirmed { get; set; }
    public int CountShipped { get; set; }
    public int CountDelivered { get; set; }
    public int CountCancelled { get; set; }
    public int CountCodPending { get; set; }
}