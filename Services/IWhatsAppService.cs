using ValousWorld.Web.Models.Entities;

namespace ValousWorld.Web.Services;

public interface IWhatsAppService
{
    Task SendOrderConfirmationAsync(Order order);
    Task SendOrderStatusUpdateAsync(Order order);
}