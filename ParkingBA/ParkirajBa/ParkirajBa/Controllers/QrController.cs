using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;

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

        [HttpPost("entry")]
        public async Task<IActionResult> Entry(string code)
        {
            var ticket = await _database.Tickets
                .FirstOrDefaultAsync(t =>
                    t.ReservationCode == code &&
                    t.QrCodeActive);

            if (ticket == null)
                return BadRequest("Nevažeći QR kod");

            if (!ticket.EnteredParking)
            {
                ticket.EnteredParking = true;
                ticket.EnteredAt = DateTime.Now;

                await _database.SaveChangesAsync();
            }

            return Ok("Ulaz dozvoljen");
        }


        [HttpPost("exit")]
        public async Task<IActionResult> Exit(string code)
        {
            var ticket = await _database.Tickets
                .Include(t => t.ParkingObject)
                .FirstOrDefaultAsync(t =>
                    t.ReservationCode == code &&
                    t.QrCodeActive);

            if (ticket == null)
                return BadRequest("Nevažeći QR kod");

            if (ticket.ExpiresAt < DateTime.Now)
            {
                return BadRequest(
                    "Rezervacija je istekla. Potrebna je doplata.");
            }

            if (!ticket.AdditionalChargePaid)
            {
                return BadRequest("Dodatna naknada nije plaćena.");
            }

            ticket.ExitedParking = true;

            ticket.ExitedAt = DateTime.Now;

            ticket.QrCodeActive = false;

            await _database.SaveChangesAsync();

            return Ok("Izlaz dozvoljen");
        }
    }
}