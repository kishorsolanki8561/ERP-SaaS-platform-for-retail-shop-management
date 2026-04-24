#pragma warning disable CS9107
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Masters;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Masters.Services;

public sealed class MasterDataService(
    PlatformDbContext db,
    IErrorLogger errorLogger)
    : BaseService<PlatformDbContext>(db, errorLogger), IMasterDataService
{
    public Task<IReadOnlyList<CountryDto>> ListCountriesAsync(CancellationToken ct = default)
        => db.Countries
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CountryDto(c.Id, c.Code, c.Name, c.PhoneCode, c.CurrencyCode))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<CountryDto>)t.Result, ct);

    public Task<IReadOnlyList<StateDto>> ListStatesByCountryAsync(long countryId, CancellationToken ct = default)
        => db.States
            .Where(s => s.CountryId == countryId && s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new StateDto(s.Id, s.Code, s.Name, s.GstStateCode, s.CountryId))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<StateDto>)t.Result, ct);

    public Task<IReadOnlyList<CityDto>> ListCitiesByStateAsync(long stateId, CancellationToken ct = default)
        => db.Cities
            .Where(c => c.StateId == stateId && c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CityDto(c.Id, c.Name, c.StateId))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<CityDto>)t.Result, ct);

    public Task<IReadOnlyList<CurrencyDto>> ListCurrenciesAsync(CancellationToken ct = default)
        => db.Currencies
            .Where(c => c.IsActive)
            .OrderBy(c => c.Code)
            .Select(c => new CurrencyDto(c.Id, c.Code, c.Name, c.Symbol, c.DecimalPlaces))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<CurrencyDto>)t.Result, ct);

    public Task<IReadOnlyList<HsnSacDto>> SearchHsnSacAsync(string query, CancellationToken ct = default)
        => db.HsnSacCodes
            .Where(h => h.IsActive && (h.Code.Contains(query) || h.Description.Contains(query)))
            .OrderBy(h => h.Code)
            .Take(50)
            .Select(h => new HsnSacDto(h.Id, h.Code, h.Description, h.Type.ToString(), h.GstRate))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<HsnSacDto>)t.Result, ct);

    public async Task<Result<long>> CreateCountryAsync(
        string code, string name, string? phoneCode, string? currencyCode, CancellationToken ct = default)
    {
        return await ExecuteAsync<long>("Masters.CreateCountry", async () =>
        {
            if (await db.Countries.AnyAsync(c => c.Code == code, ct))
                return Result<long>.Conflict(Errors.Masters.CountryConflict(code));

            var entity = new Country
            {
                Code = code, Name = name, PhoneCode = phoneCode,
                CurrencyCode = currencyCode, IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Countries.Add(entity);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<long>> CreateStateAsync(
        long countryId, string code, string name, string? gstCode, CancellationToken ct = default)
    {
        return await ExecuteAsync<long>("Masters.CreateState", async () =>
        {
            if (await db.States.AnyAsync(s => s.CountryId == countryId && s.Code == code, ct))
                return Result<long>.Conflict(Errors.Masters.StateConflict(code));

            var entity = new State
            {
                CountryId = countryId, Code = code, Name = name,
                GstStateCode = gstCode, IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.States.Add(entity);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<long>> CreateCityAsync(long stateId, string name, CancellationToken ct = default)
    {
        return await ExecuteAsync<long>("Masters.CreateCity", async () =>
        {
            if (await db.Cities.AnyAsync(c => c.StateId == stateId && c.Name == name, ct))
                return Result<long>.Conflict(Errors.Masters.CityConflict(name));

            var entity = new City
            {
                StateId = stateId, Name = name, IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Cities.Add(entity);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);
    }
}
