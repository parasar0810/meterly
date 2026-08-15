using Meterly.Application.Common.Interfaces;
using Meterly.Application.Invoices.Commands.PayInvoice;
using Meterly.Domain.Entities;
using Meterly.Domain.Enums;
using Meterly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Meterly.UnitTests.Invoices;

/// <summary>
/// Tests for PayInvoiceCommandHandler.
///
/// WHAT WE TEST:
///   - Idempotency: calling pay twice with the same key returns the same result without double-charging
///   - Successful payment marks invoice as Paid and creates a Payment record
///   - Failed payment leaves invoice Open and records a Failed payment
///   - Paying an already-Paid invoice throws BusinessRuleException
///
/// TOOLS:
///   - EF Core InMemory: lets us test database interactions without a real DB
///   - Moq: mocks out IPaymentGateway and IWebhookDispatcher (external dependencies we don't own)
///
/// INTERVIEW POINT:
/// "Why do you use mocks for IPaymentGateway but InMemory for the database?"
/// The gateway calls a real external service with real money. In tests we always
/// stub it. The database is OUR code — EF Core InMemory lets us verify that our
/// LINQ queries, entity state changes, and SaveChanges work correctly without
/// the overhead of a real PostgreSQL instance.
/// </summary>
public class PayInvoiceCommandHandlerTests
{
    private MeterlyDbContext CreateDbContext()
    {
        var tenantId = Guid.NewGuid();

        // InMemory DB — a fresh unique database per test (Guid.NewGuid() prevents test pollution)
        var services = new ServiceCollection();
        services.AddDbContext<MeterlyDbContext>(opts =>
            opts.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Mock ICurrentUserService to always return our test tenant
        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(s => s.TenantId).Returns(tenantId);
        mockCurrentUser.Setup(s => s.UserId).Returns(Guid.NewGuid());
        services.AddSingleton(mockCurrentUser.Object);

        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<MeterlyDbContext>();
    }

    private (Invoice invoice, Tenant tenant) SeedPaidInvoice(MeterlyDbContext db)
    {
        var tenant = new Tenant { Name = "Test Corp" };
        db.Tenants.Add(tenant);

        var invoice = new Invoice
        {
            TenantId = tenant.Id,
            Number = "INV-0001",
            Status = InvoiceStatus.Open,
            Currency = "USD",
            SubTotal = 100m,
            Total = 100m,
            DueDate = DateTime.UtcNow.AddDays(30)
        };
        db.Invoices.Add(invoice);
        db.SaveChanges();
        return (invoice, tenant);
    }

    [Fact]
    public async Task Handle_WhenSameIdempotencyKeySubmittedTwice_ReturnsSameResultWithoutDoubleCharge()
    {
        // Arrange
        var db = CreateDbContext();
        var (invoice, tenant) = SeedPaidInvoice(db);

        var idempotencyKey = "test-idempotency-key-001";

        // Seed an existing SUCCESSFUL payment with our idempotency key
        // (simulating a previous call that already completed)
        var existingPayment = new Payment
        {
            TenantId = tenant.Id,
            InvoiceId = invoice.Id,
            Amount = 100m,
            Currency = "USD",
            Status = PaymentStatus.Succeeded,
            GatewayReference = "gw-ref-xyz",
            IdempotencyKey = idempotencyKey
        };
        db.Payments.Add(existingPayment);
        db.SaveChanges();

        var mockGateway = new Mock<IPaymentGateway>();
        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(s => s.TenantId).Returns(tenant.Id);
        var mockWebhook = new Mock<IWebhookDispatcher>();
        var mockLedger = new Mock<ILedgerService>();

        var handler = new PayInvoiceCommandHandler(db, mockGateway.Object, mockCurrentUser.Object, mockWebhook.Object, mockLedger.Object);

        // Act
        var result = await handler.Handle(new PayInvoiceCommand
        {
            InvoiceId = invoice.Id,
            IdempotencyKey = idempotencyKey
        }, CancellationToken.None);

        // Assert: returned the cached result
        result.Success.Should().BeTrue();
        result.GatewayReference.Should().Be("gw-ref-xyz");

        // The gateway should NOT have been called a second time
        mockGateway.Verify(g => g.ChargeAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenGatewaySucceeds_MarksInvoicePaidAndCreatesPayment()
    {
        // Arrange
        var db = CreateDbContext();
        var (invoice, tenant) = SeedPaidInvoice(db);

        var mockGateway = new Mock<IPaymentGateway>();
        mockGateway
            .Setup(g => g.ChargeAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult(true, "gw-ref-abc", null));

        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(s => s.TenantId).Returns(tenant.Id);
        var mockWebhook = new Mock<IWebhookDispatcher>();
        var mockLedger = new Mock<ILedgerService>();

        var handler = new PayInvoiceCommandHandler(db, mockGateway.Object, mockCurrentUser.Object, mockWebhook.Object, mockLedger.Object);

        // Act
        var result = await handler.Handle(new PayInvoiceCommand { InvoiceId = invoice.Id }, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.GatewayReference.Should().Be("gw-ref-abc");

        // Invoice should now be Paid
        var updatedInvoice = await db.Invoices.FindAsync(invoice.Id);
        updatedInvoice!.Status.Should().Be(InvoiceStatus.Paid);
        updatedInvoice.PaidAt.Should().NotBeNull();

        // A Payment record should have been created and succeeded
        var payment = await db.Payments.FirstAsync(p => p.InvoiceId == invoice.Id);
        payment.Status.Should().Be(PaymentStatus.Succeeded);

        // Ledger entries should have been posted
        mockLedger.Verify(l => l.PostPaymentEntriesAsync(tenant.Id, It.IsAny<Invoice>(), It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenGatewayFails_LeavesInvoiceOpenAndRecordsFailedPayment()
    {
        // Arrange
        var db = CreateDbContext();
        var (invoice, tenant) = SeedPaidInvoice(db);

        var mockGateway = new Mock<IPaymentGateway>();
        mockGateway
            .Setup(g => g.ChargeAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult(false, null, "Insufficient funds"));

        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(s => s.TenantId).Returns(tenant.Id);
        var mockWebhook = new Mock<IWebhookDispatcher>();
        var mockLedger = new Mock<ILedgerService>();

        var handler = new PayInvoiceCommandHandler(db, mockGateway.Object, mockCurrentUser.Object, mockWebhook.Object, mockLedger.Object);

        // Act
        var result = await handler.Handle(new PayInvoiceCommand { InvoiceId = invoice.Id }, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be("Insufficient funds");

        var updatedInvoice = await db.Invoices.FindAsync(invoice.Id);
        updatedInvoice!.Status.Should().Be(InvoiceStatus.Open); // Invoice stays Open

        var payment = await db.Payments.FirstAsync(p => p.InvoiceId == invoice.Id);
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Be("Insufficient funds");

        // Ledger should NOT be called for a failed payment
        mockLedger.Verify(l => l.PostPaymentEntriesAsync(It.IsAny<Guid>(), It.IsAny<Invoice>(), It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInvoiceAlreadyPaid_ThrowsBusinessRuleException()
    {
        // Arrange
        var db = CreateDbContext();
        var (invoice, tenant) = SeedPaidInvoice(db);

        // Mark invoice as already paid (via a different payment path)
        invoice.Status = InvoiceStatus.Paid;
        db.SaveChanges();

        var mockGateway = new Mock<IPaymentGateway>();
        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(s => s.TenantId).Returns(tenant.Id);
        var mockWebhook = new Mock<IWebhookDispatcher>();
        var mockLedger = new Mock<ILedgerService>();

        var handler = new PayInvoiceCommandHandler(db, mockGateway.Object, mockCurrentUser.Object, mockWebhook.Object, mockLedger.Object);

        // Act & Assert
        var act = async () => await handler.Handle(new PayInvoiceCommand { InvoiceId = invoice.Id }, CancellationToken.None);

        await act.Should().ThrowAsync<Meterly.Application.Common.Exceptions.BusinessRuleException>()
            .WithMessage("Invoice is already paid.");

        // Gateway should NEVER be called
        mockGateway.Verify(g => g.ChargeAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
