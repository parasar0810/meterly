using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Common;
using Meterly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meterly.Infrastructure.Data;

/// <summary>
/// The EF Core DbContext for Meterly.
///
/// WHAT IS A DBCONTEXT?
/// Think of DbContext as the "gateway" between your C# application and the
/// PostgreSQL database. It manages:
///   - Database connections (opened/closed automatically)
///   - Change tracking (knows which entities changed and need saving)
///   - Translations (converts LINQ queries to SQL)
///   - Transactions (groups multiple saves into one atomic operation)
///
/// You never write SQL directly here (mostly) — you write C# LINQ, and
/// EF Core generates the SQL for you.
///
/// WHY INHERIT FROM DBCONTEXT?
/// DbContext has all the plumbing. We inherit it and add our DbSets
/// (which represent individual tables), plus any custom behavior.
///
/// NAMING: Entities are added as DbSet properties in Phase 2.
///         Right now, MeterlyDbContext is empty but functional.
/// </summary>
public class MeterlyDbContext : DbContext, IMeterlyDbContext
{
    private readonly ICurrentUserService _currentUserService;

    public MeterlyDbContext(
        DbContextOptions<MeterlyDbContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    // DbSet properties represent the tables in our database.
    // We will query these sets using LINQ in our Application layer.
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<WebhookEndpoint> WebhookEndpoints => Set<WebhookEndpoint>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // This line tells EF Core to automatically apply all IEntityTypeConfiguration
        // classes found in this assembly. In Phase 2 we'll create a Configurations/
        // folder with one configuration class per entity. This is cleaner than
        // putting all configurations in one giant OnModelCreating method.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MeterlyDbContext).Assembly);

        // GLOBAL QUERY FILTER FOR MULTI-TENANCY
        // If the entity has a TenantId, automatically filter queries by the current user's TenantId.
        modelBuilder.Entity<User>().HasQueryFilter(u => 
            !u.IsDeleted && 
            (_currentUserService.TenantId == null || u.TenantId == _currentUserService.TenantId));

        modelBuilder.Entity<Customer>().HasQueryFilter(c => 
            !c.IsDeleted && 
            (_currentUserService.TenantId == null || c.TenantId == _currentUserService.TenantId));

        modelBuilder.Entity<Product>().HasQueryFilter(p => 
            !p.IsDeleted && 
            (_currentUserService.TenantId == null || p.TenantId == _currentUserService.TenantId));

        modelBuilder.Entity<Plan>().HasQueryFilter(p => 
            !p.IsDeleted && 
            (_currentUserService.TenantId == null || p.Product.TenantId == _currentUserService.TenantId));

        modelBuilder.Entity<Subscription>().HasQueryFilter(s => 
            !s.IsDeleted && 
            (_currentUserService.TenantId == null || s.TenantId == _currentUserService.TenantId));

        modelBuilder.Entity<Invoice>().HasQueryFilter(i => 
            !i.IsDeleted && 
            (_currentUserService.TenantId == null || i.TenantId == _currentUserService.TenantId));

        // InvoiceLineItems inherit their tenant scope through the parent Invoice.
        // We add a filter here to suppress the EF Core warning about the required end of a relationship.
        modelBuilder.Entity<InvoiceLineItem>().HasQueryFilter(li =>
            !li.Invoice.IsDeleted &&
            (_currentUserService.TenantId == null || li.Invoice.TenantId == _currentUserService.TenantId));

        modelBuilder.Entity<Payment>().HasQueryFilter(p =>
            !p.IsDeleted &&
            (_currentUserService.TenantId == null || p.TenantId == _currentUserService.TenantId));

        modelBuilder.Entity<WebhookEndpoint>().HasQueryFilter(w =>
            !w.IsDeleted &&
            (_currentUserService.TenantId == null || w.TenantId == _currentUserService.TenantId));

        modelBuilder.Entity<LedgerEntry>().HasQueryFilter(l =>
            !l.IsDeleted &&
            (_currentUserService.TenantId == null || l.TenantId == _currentUserService.TenantId));
    }

    /// <summary>
    /// Overrides SaveChanges to automatically update audit fields.
    ///
    /// HOW THIS WORKS:
    /// EF Core's ChangeTracker knows which entities have been Added, Modified,
    /// or Deleted. Before persisting to the database, we iterate through them
    /// and update CreatedAt / UpdatedAt automatically. This means application
    /// code never has to manually set these fields — just call SaveChangesAsync()
    /// and they're handled.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;

                case EntityState.Modified:
                    // Don't overwrite CreatedAt on updates
                    entry.Property(x => x.CreatedAt).IsModified = false;
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
