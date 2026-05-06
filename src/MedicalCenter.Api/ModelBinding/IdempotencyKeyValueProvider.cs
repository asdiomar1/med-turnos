using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MedicalCenter.Api.ModelBinding;

public sealed class IdempotencyKeyValueProvider(IHeaderDictionary headers) : IValueProvider
{
    public bool ContainsPrefix(string prefix) =>
        string.Equals(prefix, "IdempotencyKey", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(prefix, "Idempotency-Key", StringComparison.OrdinalIgnoreCase);

    public ValueProviderResult GetValue(string key)
    {
        if (!ContainsPrefix(key))
            return ValueProviderResult.None;

        return headers.TryGetValue("Idempotency-Key", out var values)
            ? new ValueProviderResult(values.ToString())
            : ValueProviderResult.None;
    }
}
