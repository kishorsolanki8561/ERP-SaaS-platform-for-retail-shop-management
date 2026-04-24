namespace ErpSaas.Shared.Catalog;

public interface IServiceCatalog
{
    void Register(ServiceDescriptorEntry entry);
    IReadOnlyList<ServiceDescriptorEntry> GetAll();
}
