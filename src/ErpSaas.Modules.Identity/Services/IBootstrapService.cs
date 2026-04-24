using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Identity.Services;

public record RegisterOwnerDto(string Name, string Email, string Password);

public interface IBootstrapService
{
    Task<bool> HasProductOwnerAsync(CancellationToken ct = default);
    Task<Result<long>> RegisterProductOwnerAsync(RegisterOwnerDto dto, CancellationToken ct = default);
}
