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
            string parkingName = ticket.ParkingObject?.name ?? "ParkirajBa Parking";
            string vrijediDo = ticket.ExpiresAt.HasValue
                ? ticket.ExpiresAt.Value.ToString("dd.MM.yyyy HH:mm")
                : "—";
            string korisnikIme = ticket.ApplicationUser?.FirstName ?? "korisnik";

            await emailSender.SendEmailAsync(
                ticket.ApplicationUser.Email!,
                "⚠️ Dodatna naknada za prekoračenje — ParkirajBa",
                $"""
                <div style="font-family:'Segoe UI',Arial,sans-serif;max-width:560px;margin:0 auto;background:#f9fafb;padding:32px 20px;">
                <div style="background:white;border-radius:16px;padding:32px;box-shadow:0 2px 12px rgba(0,0,0,0.08);">

                    <div style="background:#fff3cd;border:1px solid #ffc107;border-radius:10px;padding:14px 18px;margin-bottom:24px;text-align:center;">
                        <span style="font-size:28px;">⚠️</span>
                        <p style="margin:8px 0 0;font-size:16px;font-weight:700;color:#856404;">Prekoračenje vremena parkinga</p>
                    </div>

                    <p style="color:#444;font-size:15px;margin-bottom:20px;">
                        Poštovani/a <strong>{korisnikIme}</strong>,
                    </p>
                    <p style="color:#555;font-size:14px;line-height:1.6;margin-bottom:20px;">
                        Vaša rezervacija za parking <strong>{parkingName}</strong> je istekla u <strong>{vrijediDo}</strong>,
                        ali vaše vozilo još uvijek nije evidentirano kao izašlo iz parkinga.
                    </p>

                    <div style="background:#f4f7ff;border-radius:12px;padding:18px 20px;margin-bottom:24px;">
                        <div style="display:flex;justify-content:space-between;margin-bottom:8px;font-size:14px;">
                            <span style="color:#888;">Parking </span>
                            <span style="font-weight:600;color:#333;">{parkingName}</span>
                        </div>
                        <div style="display:flex;justify-content:space-between;margin-bottom:8px;font-size:14px;">
                            <span style="color:#888;">Rezervacija istekla</span>
                            <span style="font-weight:600;color:#333;">{vrijediDo}</span>
                        </div>
                        <hr style="border:none;border-top:1px solid #e2e9ff;margin:10px 0;" />
                        <div style="display:flex;justify-content:space-between;font-size:16px;">
                            <span style="color:#333;font-weight:600;">Dodatna naknada</span>
                            <span style="font-weight:700;color:#dc3545;">{additionalPrice:0.00} KM</span>
                        </div>
                    </div>

                    <p style="color:#555;font-size:14px;line-height:1.6;margin-bottom:24px;">
                        Nakon uplate dodatne naknade, vaš QR kod će biti ponovo aktiviran i moći ćete napustiti parking.
                    </p>

                    <div style="border-top:1px solid #f0f0f0;padding-top:16px;text-align:center;">
                        <p style="color:#aaa;font-size:12px;margin:0;">
                            Hvala što koristite <strong>ParkirajBa</strong>
                        </p>
                    </div>
                </div>
                </div>
                """
            );

            ticket.OverstayEmailSent = true;
            await db.SaveChangesAsync();
        }
    }
}