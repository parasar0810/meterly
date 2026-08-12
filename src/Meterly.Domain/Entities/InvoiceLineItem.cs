using Meterly.Domain.Common;

namespace Meterly.Domain.Entities;

/// <summary>
/// Represents a single line on an invoice.
/// 
/// WHY A SEPARATE ENTITY?
/// Real invoices have multiple charges: the plan subscription fee, usage overages,
/// prorated credits, taxes, etc. By breaking these into line items, we can represent
/// complex billing scenarios while keeping the Invoice entity clean.
/// 
/// Total = Quantity * UnitAmount
/// </summary>
public class InvoiceLineItem : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitAmount { get; set; }
    public decimal TotalAmount { get; set; }
}
