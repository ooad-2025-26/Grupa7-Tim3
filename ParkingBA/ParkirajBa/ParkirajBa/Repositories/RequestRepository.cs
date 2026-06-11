using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;

namespace ParkirajBa.Repositories
{
    public class RequestRepository : IRequestRepository
    {
        private readonly ApplicationDbContext _Database;

        public RequestRepository(ApplicationDbContext database) {
            _Database = database;
        }

        public async Task AddParkingRequestAsync(ParkingRequest Request)
        {
            _Database.ParkingRequest.Add(Request);
            await _Database.SaveChangesAsync();
        }
        public async Task<ParkingRequest> AddParkingRequestAsync(int ParkingId)
        {
            ParkingRequest request = new ParkingRequest
            {
                ParkingID = ParkingId,
                TimeSent = DateTime.UtcNow
            };
            _Database.ParkingRequest.Add(request);
            await _Database.SaveChangesAsync();
            return request;
        }

    }
}
