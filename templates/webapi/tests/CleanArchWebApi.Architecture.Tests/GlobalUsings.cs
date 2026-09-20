global using System.Text.RegularExpressions;
global using ArchUnitNET.Domain;
global using static ArchUnitNET.Fluent.ArchRuleDefinition;
global using ArchUnitNET.Loader;
global using ArchUnitNET.xUnit;
global using CleanArchWebApi.Application;
global using CleanArchWebApi.Domain.Common.Interfaces;
global using CleanArchWebApi.Infrastructure.DependencyInjection;
global using Dorn.Messaging.Contracts;
global using Xunit;
// "Architecture" collides with this project's own namespace segment
// (CleanArchWebApi.Architecture.Tests) — alias to the ArchUnitNET model type explicitly.
global using ArchitectureModel = ArchUnitNET.Domain.Architecture;
