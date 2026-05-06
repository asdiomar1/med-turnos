using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MedicalCenter.Api.ModelBinding;

public sealed class IdempotencyKeyValueProviderFactory : IValueProviderFactory
{
    public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
    {
        context.ValueProviders.Add(
            new IdempotencyKeyValueProvider(context.ActionContext.HttpContext.Request.Headers));

        return Task.CompletedTask;
    }
}
