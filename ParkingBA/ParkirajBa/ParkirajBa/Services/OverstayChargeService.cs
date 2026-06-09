using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using ParkirajBa.Data;
using ParkirajBa.Models;

namespace ParkirajBa.Services;

public class OverstayChargeService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public OverstayChargeService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessExpiredTickets();

            await Task.Delay(
                TimeSpan.FromMinutes(1),
                stoppingToken);
        }
    }

    private async Task ProcessExpiredTickets()
    {
        using var scope = _scopeFactory.CreateScope();

        var db =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var emailSender =
            scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var tickets = await db.Tickets
            .Include(t => t.ApplicationUser)
            .Include(t => t.ParkingObject)
            .Where(t =>
                   t.EnteredParking &&
                   !t.ExitedParking &&
                   t.ExpiresAt != null &&
                   t.ExpiresAt < DateTime.Now)
            .ToListAsync();

        foreach (var ticket in tickets)
        {
            await CalculateAdditionalCharge(
                ticket,
                db,
                emailSender);
        }
    }

    private async Task CalculateAdditionalCharge(
        Ticket ticket,
        ApplicationDbContext db,
        IEmailSender emailSender)
    {
        var parkingPricing = await db.Pricing
            .Where(p =>
                p.ParkingObjectID == ticket.ParkingObjectId &&
                p.pricingType == PricingType.Hourly)
            .OrderByDescending(p => p.validFrom)
            .FirstOrDefaultAsync();

        if (parkingPricing == null)
            return;

        var overtime =
            DateTime.Now - ticket.ExpiresAt!.Value;

        var overtimeHours =
            (int)Math.Ceiling(overtime.TotalHours);

        if (overtimeHours <= 0)
            return;

        decimal additionalPrice =
            overtimeHours * parkingPricing.price;

        if (ticket.AdditionalCharge != additionalPrice)
        {
            ticket.AdditionalCharge = additionalPrice;
        }

        ticket.AdditionalChargePaid =
            false;

        ticket.QrCodeActive =
            false;

        await db.SaveChangesAsync();

        if (!ticket.OverstayEmailSent)
        {
            await emailSender.SendEmailAsync(
            ticket.ApplicationUser.Email!,
            "Dodatna naknada za parking",
            $"""
        Poštovani,

        Vaša rezervacija je istekla,
        ali vozilo nije evidentirano kao izašlo.

        Dodatna naknada:
        {additionalPrice:0.00} KM

        Nakon uplate dobit ćete novi QR kod.

        ParkirajBa
        """);
            ticket.OverstayEmailSent = true;
            await db.SaveChangesAsync();
        }
    }
}