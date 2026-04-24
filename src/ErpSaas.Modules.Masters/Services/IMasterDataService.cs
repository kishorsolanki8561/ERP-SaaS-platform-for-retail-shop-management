using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Masters.Services;

public record CountryDto(long Id, string Code, string Name, string? PhoneCode, string? CurrencyCode);
public record StateDto(long Id, string Code, string Name, string? GstStateCode, long CountryId);
public record CityDto(long Id, string Name, long StateId);
public record CurrencyDto(long Id, string Code, string Name, string Symbol, int DecimalPlaces);
public record HsnSacDto(long Id, string Code, string Description, string Type, decimal? GstRate);

public interface IMasterDataService
{
    Task<IReadOnlyList<CountryDto>> ListCountriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StateDto>> ListStatesByCountryAsync(long countryId, CancellationToken ct = default);
    Task<IReadOnlyList<CityDto>> ListCitiesByStateAsync(long stateId, CancellationToken ct = default);
    Task<IReadOnlyList<CurrencyDto>> ListCurrenciesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<HsnSacDto>> SearchHsnSacAsync(string query, CancellationToken ct = default);

    Task<Result<long>> CreateCountryAsync(string code, string name, string? phoneCode, string? currencyCode, CancellationToken ct = default);
    Task<Result<long>> CreateStateAsync(long countryId, string code, string name, string? gstCode, CancellationToken ct = default);
    Task<Result<long>> CreateCityAsync(long stateId, string name, CancellationToken ct = default);
}
