using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Hubs;

namespace ParkirajBa.Services
{
    public class ParkingReservationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ParkingReservationBackgroundService> _logger;
        private readonly IHubContext<ParkingHub> _hubContext;

        public ParkingReservationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ParkingReservationBackgroundService> logger,
            IHubContext<ParkingHub> hubContext)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessReservations();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Greška u ParkingReservationBackgroundService");
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(30),
                    stoppingToken);
            }
        }

        private async Task ProcessReservations()
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var now = DateTime.Now;

            var tickets = await db.Tickets
                .Include(t => t.ParkingObject)
                .Where(t =>
                    t.IsPaid &&
                    !t.IsCancelled &&
                    !t.SpotCountApplied &&
                    t.IssuedAt <= now)
                .ToListAsync();

            foreach (var ticket in tickets)
            {
                if (ticket.ParkingObject == null)
                    continue;

                if (ticket.ParkingObject.availableSpots > 0)
                {
                    ticket.ParkingObject.availableSpots--;

                    await _hubContext.Clients.All.SendAsync(
                            "ParkingSpotsChanged",
                            ticket.ParkingObjectId,
                            ticket.ParkingObject.availableSpots);

                    ticket.SpotCountApplied = true;

                    _logger.LogInformation(
                        $"Smanjen broj mjesta za ticket {ticket.Id}");
                }
            }

            await db.SaveChangesAsync();
        }
    }
}