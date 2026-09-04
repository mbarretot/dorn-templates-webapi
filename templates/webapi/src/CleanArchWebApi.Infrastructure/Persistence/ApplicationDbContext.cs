#if (UseCustomAuth)
using CleanArchWebApi.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.ChangeTracking;
#endif

namespace CleanArchWebApi.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IPublisher _publisher;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IPublisher publisher
    )
        : base(options)
    {
        _publisher = publisher;
    }

    public DbSet<TodoItem> Items => Set<TodoItem>();

#if (UseCustomAuth)
    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
#endif

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregatesWithEvents = ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var aggregate in aggregatesWithEvents)
        {
            var domainEvents = aggregate.DomainEvents.ToArray();
            aggregate.ClearDomainEvents();

            foreach (var domainEvent in domainEvents)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
        }

        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>(builder =>
        {
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Title).IsRequired().HasMaxLength(200);
        });

#if (UseCustomAuth)
        modelBuilder.Entity<AppUser>(builder =>
        {
            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => u.NormalizedEmail).IsUnique();

            // Stored as a single delimited column instead of a join table -- permissions are a small, closed set
            // owned entirely by this entity, so a normalized table would add a join for no real benefit here.
            builder
                .Property(u => u.Permissions)
                .HasConversion(
                    permissions => string.Join(',', permissions),
                    value =>
                        value.Length == 0
                            ? Array.Empty<string>()
                            : value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                )
                .Metadata.SetValueComparer(
                    new ValueComparer<string[]>(
                        (a, b) =>
                            (a ?? Array.Empty<string>()).SequenceEqual(b ?? Array.Empty<string>()),
                        a =>
                            a.Aggregate(
                                0,
                                (hash, permission) => HashCode.Combine(hash, permission)
                            ),
                        a => a.ToArray()
                    )
                );
        });

        modelBuilder.Entity<RefreshToken>(builder =>
        {
            builder.HasKey(token => token.Id);
            builder.Property(token => token.TokenHash).IsRequired();
            builder.HasIndex(token => token.TokenHash).IsUnique();
            builder.HasIndex(token => token.UserId);
            builder
                .HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
#endif

        base.OnModelCreating(modelBuilder);
    }
}
