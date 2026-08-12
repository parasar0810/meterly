namespace Meterly.Domain.Common;

/// <summary>
/// Marks an entity as having soft-delete support.
///
/// WHY SOFT DELETE?
/// In a billing system, you should almost never permanently delete records.
/// If a customer deletes their account, you still need their invoices and
/// payment records for legal/audit reasons. Soft delete just hides the
/// record rather than removing it from the database.
///
/// HOW IT WORKS:
/// Entities implementing this interface have an IsDeleted flag. We configure
/// EF Core to automatically filter out deleted records using a "global query
/// filter" (added in Phase 2). This means most queries just "work" without
/// explicitly checking IsDeleted everywhere.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
