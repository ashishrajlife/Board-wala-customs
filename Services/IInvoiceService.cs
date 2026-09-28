using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public interface IInvoiceService
{
    byte[] GenerateInvoice(Order order);
    string GetInvoiceFileName(Order order);
}