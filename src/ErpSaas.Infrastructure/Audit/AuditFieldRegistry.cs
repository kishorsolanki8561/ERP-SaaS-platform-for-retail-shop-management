using System.Collections.Concurrent;
using System.Text.Json;
using ErpSaas.Shared.Services;

namespace ErpSaas.Infrastructure.Audit;

public sealed record AuditFieldMeta(string PropertyName, string DisplayName);

public sealed record AuditChangedField(string Field, string DisplayName, string? OldValue, string? NewValue);

/// <summary>
/// Caches [AuditField]-decorated property metadata per entity type name.
/// Used to filter OldValues/NewValues JSON down to only screen-visible fields.
/// </summary>
public static class AuditFieldRegistry
{
    private static readonly ConcurrentDictionary<string, IReadOnlyList<AuditFieldMeta>> _cache = new();

    public static IReadOnlyList<AuditFieldMeta> GetFields(Type entityType)
    {
        return _cache.GetOrAdd(entityType.Name, _ => BuildFields(entityType));
    }

    public static IReadOnlyList<AuditFieldMeta> GetFields(string entityTypeName)
    {
        if (_cache.TryGetValue(entityTypeName, out var cached))
            return cached;
        // Try to resolve from loaded assemblies
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
            .FirstOrDefault(t => t.Name == entityTypeName);
        return type is null ? [] : _cache.GetOrAdd(entityTypeName, _ => BuildFields(type));
    }

    private static IReadOnlyList<AuditFieldMeta> BuildFields(Type type)
    {
        return type.GetProperties()
            .Select(p => (p, attr: p.GetCustomAttributes(typeof(AuditFieldAttribute), inherit: true)
                                    .OfType<AuditFieldAttribute>()
                                    .FirstOrDefault()))
            .Where(x => x.attr is not null)
            .Select(x => new AuditFieldMeta(x.p.Name, x.attr!.DisplayName))
            .ToList();
    }

    /// <summary>
    /// Computes a field-level diff between oldJson and newJson, returning only [AuditField] fields
    /// that actually changed. For Insert events pass oldJson=null; for Delete events pass newJson=null.
    /// </summary>
    public static IReadOnlyList<AuditChangedField> ComputeDiff(
        string entityTypeName,
        string? oldJson,
        string? newJson)
    {
        var fields = GetFields(entityTypeName);
        if (fields.Count == 0) return [];

        var oldMap = ParseJson(oldJson);
        var newMap = ParseJson(newJson);

        var result = new List<AuditChangedField>();
        foreach (var f in fields)
        {
            oldMap.TryGetValue(f.PropertyName, out var oldVal);
            newMap.TryGetValue(f.PropertyName, out var newVal);

            // For pure inserts, always include the new value. For updates, only include changed fields.
            bool isInsert = oldJson is null && newJson is not null;
            bool isDelete = oldJson is not null && newJson is null;
            bool changed  = oldVal != newVal;

            if (isInsert || isDelete || changed)
                result.Add(new AuditChangedField(f.PropertyName, f.DisplayName, oldVal, newVal));
        }
        return result;
    }

    private static Dictionary<string, string?> ParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
