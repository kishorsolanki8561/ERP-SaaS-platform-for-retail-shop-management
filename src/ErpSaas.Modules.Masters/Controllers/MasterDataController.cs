using ErpSaas.Modules.Masters.Services;
using ErpSaas.Shared.Authorization;
using ErpSaas.Shared.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpSaas.Modules.Masters.Controllers;

[Route("api/masters")]
[Authorize]
public sealed class MasterDataController(IMasterDataService masterService) : BaseController
{
    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries(CancellationToken ct)
        => Ok(await masterService.ListCountriesAsync(ct));

    [HttpGet("countries/{countryId:long}/states")]
    public async Task<IActionResult> GetStates(long countryId, CancellationToken ct)
        => Ok(await masterService.ListStatesByCountryAsync(countryId, ct));

    [HttpGet("states/{stateId:long}/cities")]
    public async Task<IActionResult> GetCities(long stateId, CancellationToken ct)
        => Ok(await masterService.ListCitiesByStateAsync(stateId, ct));

    [HttpGet("currencies")]
    public async Task<IActionResult> GetCurrencies(CancellationToken ct)
        => Ok(await masterService.ListCurrenciesAsync(ct));

    [HttpGet("hsn-sac")]
    public async Task<IActionResult> SearchHsnSac([FromQuery] string q, CancellationToken ct)
        => Ok(await masterService.SearchHsnSacAsync(q ?? "", ct));

    [HttpPost("countries")]
    [RequirePermission("MasterData.Manage")]
    public async Task<IActionResult> CreateCountry([FromBody] CreateCountryRequest req, CancellationToken ct)
        => Ok(await masterService.CreateCountryAsync(req.Code, req.Name, req.PhoneCode, req.CurrencyCode, ct));

    [HttpPost("countries/{countryId:long}/states")]
    [RequirePermission("MasterData.Manage")]
    public async Task<IActionResult> CreateState(long countryId, [FromBody] CreateStateRequest req, CancellationToken ct)
        => Ok(await masterService.CreateStateAsync(countryId, req.Code, req.Name, req.GstStateCode, ct));

    [HttpPost("states/{stateId:long}/cities")]
    [RequirePermission("MasterData.Manage")]
    public async Task<IActionResult> CreateCity(long stateId, [FromBody] CreateCityRequest req, CancellationToken ct)
        => Ok(await masterService.CreateCityAsync(stateId, req.Name, ct));
}

public record CreateCountryRequest(string Code, string Name, string? PhoneCode, string? CurrencyCode);
public record CreateStateRequest(string Code, string Name, string? GstStateCode);
public record CreateCityRequest(string Name);
