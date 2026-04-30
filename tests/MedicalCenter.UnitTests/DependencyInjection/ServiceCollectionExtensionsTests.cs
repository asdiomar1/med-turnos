using MedicalCenter.Infrastructure.DependencyInjection;
using MedicalCenter.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalCenter.UnitTests.DependencyInjection;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInfrastructure_RegistersRateLimitingOptions()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddInfrastructure(config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RateLimitingOptions>>();
        Assert.NotNull(options.Value);
    }

    [Fact]
    public void AddInfrastructure_RegistersApiKeyOptions()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddInfrastructure(config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiKeyOptions>>();
        Assert.NotNull(options.Value);
    }

    [Fact]
    public void AddInfrastructure_RegistersRateLimiter_WithConfigurableValues()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:AuthPermitLimit"] = "10",
                ["RateLimiting:AuthWindowSeconds"] = "120"
            })
            .Build();

        services.AddInfrastructure(config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RateLimitingOptions>>();
        Assert.Equal(10, options.Value.AuthPermitLimit);
        Assert.Equal(120, options.Value.AuthWindowSeconds);
    }
}
