using Xunit;

namespace Dorn.Templates.WebApi.Tests;

[CollectionDefinition(Name)]
public sealed class TemplatePackCollection : ICollectionFixture<TemplatePackFixture>
{
    public const string Name = "TemplatePack";
}

public sealed class TemplatePackFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await TemplatePackHarness.InstallAsync("Dorn.Templates.WebApi");
    }

    public async Task DisposeAsync()
    {
        await TemplatePackHarness.UninstallAsync("Dorn.Templates.WebApi");
    }
}
