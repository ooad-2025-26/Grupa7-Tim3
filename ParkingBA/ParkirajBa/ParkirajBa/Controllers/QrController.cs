using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;
using Microsoft.AspNetCore.SignalR;
using ParkirajBa.Hubs;

namespace ParkirajBa.Controllers
{
    [ApiController]
    [Route("api/qr")]
    public class QrController : ControllerBase
    {
        private readonly ApplicationDbContext _database;
        private readonly IHubContext<ParkingHub> _hub;
        public QrController(ApplicationDbContext database, IHubContext<ParkingHub> hub)
        {
            _database = database;
            _hub = hub;
        }
        [HttpPost("scan")]
        public async Task<IActionResult> Scan([FromBody] QrRequest request)
        {
            var ticket = await _database.Tickets
                .FirstOrDefaultAsync(t =>
                    t.ReservationCode == request.Code &&
                    t.QrCodeActive);

            if (ticket == null)
                return BadRequest(new { message = "Nevažeći QR kod" });

            // ENTRY LOGIC
            if (!ticket.EnteredParking)
            {
                if (!ticket.IsPaid)
                    return BadRequest(new { message = "Nije plaćeno" });

                ticket.EnteredParking = true;
                ticket.EnteredAt = DateTime.Now;

                await _database.SaveChangesAsync();
                await _hub.Clients.All.SendAsync("StatusChanged", request.Code);

                return Ok(new { message = "Ulaz dozvoljen" });
            }

            // EXIT LOGIC
            if (ticket.EnteredParking && !ticket.ExitedParking)
            {
                if (ticket.ExpiresAt < DateTime.Now)
                    return BadRequest(new { message = "Rezervacija istekla" });

                if (!ticket.AdditionalChargePaid)
                    return BadRequest(new { message = "Dodatna naknada nije plaćena" });

                ticket.ExitedParking = true;
                ticket.ExitedAt = DateTime.Now;
                ticket.QrCodeActive = false;

                await _database.SaveChangesAsync();
                await _hub.Clients.All.SendAsync("StatusChanged", request.Code);

                return Ok(new { message = "Izlaz dozvoljen" });
            }

            return BadRequest(new { message = "Parking već završen" });
        }


        //Statusni
        [HttpGet("status/{code}")]
        public async Task<IActionResult> Status(string code)
        {
            var ticket = await _database.Tickets
                .FirstOrDefaultAsync(t => t.ReservationCode == code);

            if (ticket == null)
                return NotFound();

            return Ok(new
            {
                entered = ticket.EnteredParking,
                exited = ticket.ExitedParking
            });
        }
    }
}