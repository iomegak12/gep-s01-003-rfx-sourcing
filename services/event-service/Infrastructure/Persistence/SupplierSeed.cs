using EventService.Domain.Entities;

namespace EventService.Infrastructure.Persistence;

/// <summary>
/// Hard-coded supplier master list used when <c>Persistence:SeedDataEnabled</c>
/// is true and the suppliers table is empty. The dataset is intentionally
/// representative for the demo (six Indian-context suppliers).
/// </summary>
internal static class SupplierSeed
{
    public static IReadOnlyList<Supplier> Records { get; } = new List<Supplier>
    {
        New("Acme Cloud Pvt Ltd",         "sales@acmecloud.example"),
        New("Bharat Office Supplies",      "contact@bharatoffice.example"),
        New("Coromandel Logistics",        "orders@coromandel-logi.example"),
        New("DeltaPrint Services",         "hello@deltaprint.example"),
        New("Everest Networking Solutions","sales@everestnet.example"),
        New("Fortis Industrial Hardware",  "info@fortishw.example")
    };

    private static Supplier New(string name, string email) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name,
        ContactEmail = email,
        IsActive = true,
        CreatedAtUtc = DateTime.UtcNow
    };
}
