#if (UseCustomAuth)
using CleanArchWebApi.Domain.Users;
using Microsoft.AspNetCore.Identity;
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
