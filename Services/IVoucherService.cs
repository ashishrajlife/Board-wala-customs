namespace ValousWorld.Web.Services;

public sealed record VoucherValidationResult(
    bool IsValid,
    string Message,
    decimal DiscountAmount);

public interface IVoucherService
{
    Task<VoucherValidationResult> ValidateAsync(int userId, string? code);
}