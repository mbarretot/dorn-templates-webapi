namespace CleanArchWebApi.Architecture.Tests;

/// <summary>
/// Enforces the layering rules in README.md ("Capas") as executable checks (ArchUnitNET,
/// see ADR 0012), since nothing stops an errant `using` from compiling otherwise.
/// </summary>
public sealed class LayeringTests
{
    private static readonly ArchitectureModel Architecture = new ArchLoader()
        .LoadAssembliesIncludingDependencies(
            typeof(TodoItem).Assembly,
            typeof(CreateTodoItemCommand).Assembly,
            typeof(ServiceCollectionExtensions).Assembly,
            typeof(Program).Assembly
        )
        .Build();

    private static IObjectProvider<IType> InNamespace(string root) =>
        Types().That().ResideInNamespaceMatching($@"^{Regex.Escape(root)}(\.|$)");

    private static readonly IObjectProvider<IType> Domain = InNamespace("CleanArchWebApi.Domain");
    private static readonly IObjectProvider<IType> Application = InNamespace(
        "CleanArchWebApi.Application"
    );
    private static readonly IObjectProvider<IType> WebApi = InNamespace("CleanArchWebApi.WebApi");

    [Fact]
    public void Domain_ShouldNot_DependOnApplicationInfrastructureOrWebApi()
    {
        Types()
            .That()
            .Are(Domain)
            .Should()
            .NotDependOnAny(
                Types()
                    .That()
                    .ResideInNamespaceMatching(
                        @"^CleanArchWebApi\.(Application|Infrastructure|WebApi)(\.|$)"
                    )
            )
            .Check(Architecture);
    }

    [Fact]
    public void Domain_ShouldNot_DependOnEntityFrameworkCore()
    {
        Types()
            .That()
            .Are(Domain)
            .Should()
            .NotDependOnAny(
                Types().That().ResideInNamespaceMatching(@"^Microsoft\.EntityFrameworkCore")
            )
            .Check(Architecture);
    }

    [Fact]
    public void Application_ShouldNot_DependOnInfrastructureOrWebApi()
    {
        Types()
            .That()
            .Are(Application)
            .Should()
            .NotDependOnAny(
                Types()
                    .That()
                    .ResideInNamespaceMatching(@"^CleanArchWebApi\.(Infrastructure|WebApi)(\.|$)")
            )
            .Check(Architecture);
    }

    [Fact]
    public void Infrastructure_ShouldNot_DependOnWebApi()
    {
        Types()
            .That()
            .ResideInNamespaceMatching(@"^CleanArchWebApi\.Infrastructure(\.|$)")
            .Should()
            .NotDependOnAny(Types().That().Are(WebApi))
            .Check(Architecture);
    }

    [Fact]
    public void RequestHandlers_Should_ResideInApplicationAssembly()
    {
        // ArchUnitNET's fluent predicates don't reliably target open generic interfaces, so
        // this one rule uses plain reflection instead.
        var handlerTypes = new[]
        {
            typeof(TodoItem).Assembly,
            typeof(CreateTodoItemCommand).Assembly,
            typeof(ServiceCollectionExtensions).Assembly,
            typeof(Program).Assembly,
        }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type =>
                type.GetInterfaces()
                    .Any(i =>
                        i.IsGenericType
                        && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                    )
            )
            .ToList();

        Assert.NotEmpty(handlerTypes);
        Assert.All(
            handlerTypes,
            handlerType =>
                Assert.StartsWith(
                    "CleanArchWebApi.Application",
                    handlerType.Namespace,
                    StringComparison.Ordinal
                )
        );
    }
}
