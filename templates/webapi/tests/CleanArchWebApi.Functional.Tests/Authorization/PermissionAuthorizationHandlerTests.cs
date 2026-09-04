#if (UseAuth)
using System.Security.Claims;
using CleanArchWebApi.Application.Common.Security;
using CleanArchWebApi.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace CleanArchWebApi.Functional.Tests.Authorization;

public sealed class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleAsync_UserHasMatchingPermissionClaim_Succeeds()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement(Permissions.TodosRead);
        var context = new AuthorizationHandlerContext(
            [requirement],
            CreatePrincipal(Permissions.TodosRead),
            resource: null
        );

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_UserLacksMatchingPermissionClaim_DoesNotSucceed()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement(Permissions.TodosDelete);
        var context = new AuthorizationHandlerContext(
            [requirement],
            CreatePrincipal(Permissions.TodosRead, Permissions.TodosWrite),
            resource: null
        );

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_UserHasNoPermissionClaimsAtAll_DoesNotSucceed()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement(Permissions.TodosRead);
        var identity = new ClaimsIdentity(authenticationType: "Test");
        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(identity),
            resource: null
        );

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static ClaimsPrincipal CreatePrincipal(params string[] permissions)
    {
        var identity = new ClaimsIdentity(
            permissions.Select(p => new Claim(Permissions.ClaimType, p)),
            authenticationType: "Test"
        );
        return new ClaimsPrincipal(identity);
    }
}
#endif
