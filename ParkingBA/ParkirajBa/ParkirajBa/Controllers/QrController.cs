using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;

namespace ParkirajBa.Controllers
{
    [ApiController]
    [Route("api/qr")]
    public class QrController : ControllerBase
    {
        private readonly ApplicationDbContext _database;

        public QrController(ApplicationDbContext database)
        {
            _database = database;
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

                return Ok(new { message = "Izlaz dozvoljen" });
            }

            return BadRequest(new { message = "Parking već završen" });
        }
    }
}