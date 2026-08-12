namespace Meterly.Domain.Common;

/// <summary>
/// Base class that every entity in Meterly inherits.
///
/// WHY THIS EXISTS:
/// Every table in our database will need an Id, a creation timestamp,
/// and an updated timestamp. Instead of repeating these three fields in
/// every entity class (which would violate the DRY principle — Don't
/// Repeat Yourself), we put them here and every entity inherits them.
///
/// CONCURRENCY TOKEN:
/// RowVersion is a byte array that EF Core uses for optimistic concurrency.
/// When two users try to update the same record simultaneously, EF Core
/// will throw a DbUpdateConcurrencyException — preventing silent overwrites.
/// This matters a lot for financial records.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Primary key. Using Guid instead of int because:
    /// 1. Guids are globally unique — safe to generate in application code
    ///    without asking the database first (important for offline scenarios).
    /// 2. They don't expose record counts to users (no "your invoice #3").
    /// 3. They distribute better across database shards if we ever scale.
    /// Downside: slightly larger than int (16 bytes vs 4 bytes). For this
    /// scale, that tradeoff is absolutely fine.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// When this record was first created. Stored in UTC.
    /// Always use UTC in backend systems — never local time. Local time
    /// changes with daylight saving, UTC never does.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last modified. Updated automatically
    /// by the DbContext SaveChanges override (we'll add that in Phase 2).
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// EF Core optimistic concurrency token.
    /// PostgreSQL will automatically manage this via xmin system column.
    /// If two requests try to update the same row, the second one will
    /// see that the RowVersion has changed and throw an exception.
    /// </summary>
    public uint RowVersion { get; set; }
}
