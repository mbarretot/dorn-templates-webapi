using CleanArchWebApi.Application.Common.Persistence;
#if (UseCustomAuth)
using CleanArchWebApi.Application.Common.Security;
#endif
#if (UseBlobStorage)
using CleanArchWebApi.Application.Common.Storage;
#endif
using CleanArchWebApi.Domain.Common.Interfaces;
#if (UseCustomAuth)
using CleanArchWebApi.Domain.Users;
using CleanArchWebApi.Infrastructure.Auth;
#endif
#if (UseCustomAuth)
using Microsoft.AspNetCore.Identity;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchWebApi.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
#if (UseEfCore)
        services.AddDbContext<ApplicationDbContext>(options =>
#if (UseSqlite)
            options.UseSqlite(configuration.GetConnectionString("Default"))
#elif (UseSqlServer)
            options.UseSqlServer(configuration.GetConnectionString("CleanArchWebApi"))
#elif (UsePostgres)
            options.UseNpgsql(configuration.GetConnectionString("CleanArchWebApi"))
#endif
        );

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>()
        );

#if (IncludeSample)
        services.AddScoped<ITodoItemRepository, Repositories.EfCore.TodoItemRepository>();
#endif
#endif

#if (UseDapper)
        services.AddScoped<Repositories.Dapper.DapperContext>();

#if (IncludeSample)
        services.AddScoped<ITodoItemRepository, Repositories.Dapper.TodoItemRepository>();
#endif
#endif

#if (UseBlobStorage)
#if (UseDatabaseBlobs)
#if (UseEfCore)
        services.AddScoped<IBlobStore, Storage.EfCoreBlobStore>();
#elif (UseDapper)
        services.AddScoped<IBlobStore, Storage.DapperBlobStore>();
#endif
#elif (UseFileSystemBlobs)
        services.AddSingleton<IBlobStore>(_ => new Storage.FileSystemBlobStore(
            configuration["BlobStorage:FileSystem:RootPath"]
                ?? throw new InvalidOperationException(
                    "BlobStorage:FileSystem:RootPath is not configured."
                )
        ));
#endif

#endif
#if (UseCustomAuth)
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AuthSeedOptions>(
            configuration.GetSection(AuthSeedOptions.SectionName)
        );
        services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
        services.AddScoped<ITokenService, JwtTokenService>();
#endif

        return services;
    }
}
