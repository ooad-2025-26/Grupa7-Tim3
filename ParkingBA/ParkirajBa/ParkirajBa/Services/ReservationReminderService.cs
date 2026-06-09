using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;

namespace ParkirajBa.Services
{
    public class ReservationReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReservationReminderService> _logger;

        public ReservationReminderService(
            IServiceScopeFactory scopeFactory,
            ILogger<ReservationReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckExpiringReservations();
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Greška prilikom provjere rezervacija.");
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
        }

        private async Task CheckExpiringReservations()
        {
            using var scope = _scopeFactory.CreateScope();

            var database =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var emailSender =
                scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var now = DateTime.Now;
            var reminderTime = now.AddMinutes(10);

            var tickets = await database.Tickets
                .Include(t => t.ApplicationUser)
                .Include(t => t.ParkingObject)
                .Where(t =>
                    t.IsPaid &&
                    !t.ExpirationReminderSent &&
                    t.ExpiresAt.HasValue &&
                    t.ExpiresAt.Value > now &&
                    t.ExpiresAt.Value <= reminderTime)
                .ToListAsync();

            foreach (var ticket in tickets)
            {
                try
                {
                    await emailSender.SendEmailAsync(
                        ticket.ApplicationUser.Email!,
                        "⏰ Rezervacija uskoro ističe",
                        $@"
                        <h2>Podsjetnik na rezervaciju</h2>

                        <p>Poštovani/a <strong>{ticket.ApplicationUser.FullName}</strong>,</p>

                        <p>
                            Vaša rezervacija za parking
                            <strong>{ticket.ParkingObject.name}</strong>
                            ističe za manje od 10 minuta.
                        </p>

                        <p>
                            <strong>Vrijedi do:</strong>
                            {ticket.ExpiresAt:dd.MM.yyyy HH:mm}
                        </p>

                        <p>
                            Ukoliko želite nastaviti koristiti parking,
                            produžite rezervaciju prije isteka.
                        </p>

                        <br/>

                        <p>Hvala što koristite ParkirajBa!</p>
                        ");

                    ticket.ExpirationReminderSent = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Greška pri slanju reminder maila za Ticket {TicketId}",
                        ticket.Id);
                }
            }

            await database.SaveChangesAsync();
        }
    }
}