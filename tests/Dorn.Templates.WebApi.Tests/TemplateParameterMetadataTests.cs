using System.Text.Json;
using Xunit;

namespace Dorn.Templates.WebApi.Tests;

/// <summary>
/// `dotnet new dorn-webapi --help` (and any GUI built on the template engine's metadata) reads
/// template.json, so its parameter contract is asserted here without generating anything.
/// </summary>
public class TemplateParameterMetadataTests
{
    private static readonly Lazy<JsonElement> Symbols = new(() =>
    {
        var path = Path.Combine(
            TemplatePackHarness.TemplatesRoot,
            ".template.config",
            "template.json"
        );
        var document = JsonDocument.Parse(
            File.ReadAllText(path),
            new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            }
        );
        return document.RootElement.GetProperty("symbols");
    });

    private static JsonElement Parameter(string name)
    {
        Assert.True(
            Symbols.Value.TryGetProperty(name, out var symbol),
            $"Missing symbol '{name}'."
        );
        Assert.Equal("parameter", symbol.GetProperty("type").GetString());
        return symbol;
    }

    [Fact]
    public void EveryParameter_HasADescription_AndEveryChoiceIsDescribed()
    {
        foreach (var symbol in Symbols.Value.EnumerateObject())
        {
            if (symbol.Value.GetProperty("type").GetString() != "parameter")
            {
                continue;
            }

            Assert.False(
                string.IsNullOrWhiteSpace(symbol.Value.GetProperty("description").GetString()),
                $"Parameter '{symbol.Name}' needs a description."
            );

            if (symbol.Value.GetProperty("datatype").GetString() == "choice")
            {
                var choices = symbol
                    .Value.GetProperty("choices")
                    .EnumerateArray()
                    .Select(choice => choice.GetProperty("choice").GetString())
                    .ToList();
                Assert.Contains(symbol.Value.GetProperty("defaultValue").GetString(), choices);
                Assert.All(
                    symbol.Value.GetProperty("choices").EnumerateArray(),
                    choice =>
                        Assert.False(
                            string.IsNullOrWhiteSpace(choice.GetProperty("description").GetString())
                        )
                );
            }
        }
    }

    [Fact]
    public void ConnectionString_IsAnOptionalStringThatDefaultsToEmpty_AndWarnsAboutSecrets()
    {
        var parameter = Parameter("ConnectionString");

        Assert.Equal("string", parameter.GetProperty("datatype").GetString());
        Assert.Equal("", parameter.GetProperty("defaultValue").GetString());
        var description = parameter.GetProperty("description").GetString()!;
        Assert.Contains("secret", description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("user-secrets", description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IncludeAgentRules_IsAnOptInBool_ThatNamesTheGeneratedFiles()
    {
        var parameter = Parameter("IncludeAgentRules");

        Assert.Equal("bool", parameter.GetProperty("datatype").GetString());
        Assert.Equal("false", parameter.GetProperty("defaultValue").GetString());
        var description = parameter.GetProperty("description").GetString()!;
        Assert.Contains("AGENTS.md", description, StringComparison.Ordinal);
        Assert.Contains("CLAUDE.md", description, StringComparison.Ordinal);
    }

    [Fact]
    public void IncludeLocalization_IsAnOptInBool_ThatNamesTheMechanism()
    {
        var parameter = Parameter("IncludeLocalization");

        Assert.Equal("bool", parameter.GetProperty("datatype").GetString());
        Assert.Equal("false", parameter.GetProperty("defaultValue").GetString());
        var description = parameter.GetProperty("description").GetString()!;
        Assert.Contains("IStringLocalizer", description, StringComparison.Ordinal);
        Assert.Contains("Accept-Language", description, StringComparison.Ordinal);
        Assert.Contains(".resx", description, StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultLanguage_IsAStringThatDefaultsToEnglish()
    {
        var parameter = Parameter("DefaultLanguage");

        Assert.Equal("string", parameter.GetProperty("datatype").GetString());
        Assert.Equal("en", parameter.GetProperty("defaultValue").GetString());
        Assert.Contains(
            "IncludeLocalization",
            parameter.GetProperty("description").GetString()!,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Languages_IsACommaSeparatedStringOfCultureCodes_ThatDefaultsToEmpty()
    {
        var parameter = Parameter("Languages");

        Assert.Equal("string", parameter.GetProperty("datatype").GetString());
        Assert.Equal("", parameter.GetProperty("defaultValue").GetString());
        var description = parameter.GetProperty("description").GetString()!;
        Assert.Contains("culture", description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("comma", description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("24", description, StringComparison.Ordinal);
    }

    [Fact]
    public void BlobStorage_IsAChoiceThatDefaultsToManual_AndNamesThePortAndTheRisks()
    {
        var parameter = Parameter("BlobStorage");

        Assert.Equal("choice", parameter.GetProperty("datatype").GetString());
        Assert.Equal("manual", parameter.GetProperty("defaultValue").GetString());
        Assert.Equal(
            ["manual", "database", "filesystem"],
            parameter
                .GetProperty("choices")
                .EnumerateArray()
                .Select(choice => choice.GetProperty("choice").GetString())
        );
        var description = parameter.GetProperty("description").GetString()!;
        Assert.Contains("IBlobStore", description, StringComparison.Ordinal);
        Assert.Contains("memory", description, StringComparison.Ordinal);
        Assert.Contains("BlobStorage:FileSystem:RootPath", description, StringComparison.Ordinal);
        Assert.Contains("escape", description, StringComparison.Ordinal);
    }

    [Fact]
    public void IncludeSample_IsAnOptOutBool_ThatDocumentsTheCustomAuthLimit()
    {
        var parameter = Parameter("IncludeSample");

        Assert.Equal("bool", parameter.GetProperty("datatype").GetString());
        Assert.Equal("true", parameter.GetProperty("defaultValue").GetString());
        var description = parameter.GetProperty("description").GetString()!;
        Assert.Contains("Todo", description, StringComparison.Ordinal);
        Assert.Contains("Auth=custom", description, StringComparison.Ordinal);
    }
}
