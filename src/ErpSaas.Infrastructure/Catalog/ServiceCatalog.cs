using ErpSaas.Shared.Catalog;

namespace ErpSaas.Infrastructure.Catalog;

// Entries declared at DI registration time are injected via IEnumerable<ServiceDescriptorEntry>.
// Register() is kept for any dynamic additions after build.
public sealed class ServiceCatalog(IEnumerable<ServiceDescriptorEntry> registeredEntries) : IServiceCatalog
{
    private readonly List<ServiceDescriptorEntry> _entries = [..registeredEntries];

    public void Register(ServiceDescriptorEntry entry) => _entries.Add(entry);
    public IReadOnlyList<ServiceDescriptorEntry> GetAll() => _entries.AsReadOnly();
}
