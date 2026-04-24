using ErpSaas.Shared.Catalog;

namespace ErpSaas.Infrastructure.Catalog;

public sealed class ServiceCatalog : IServiceCatalog
{
    private readonly List<ServiceDescriptorEntry> _entries = [];

    public void Register(ServiceDescriptorEntry entry) => _entries.Add(entry);

    public IReadOnlyList<ServiceDescriptorEntry> GetAll() => _entries.AsReadOnly();
}
