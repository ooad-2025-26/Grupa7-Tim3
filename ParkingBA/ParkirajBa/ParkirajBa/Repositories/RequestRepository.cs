using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;

namespace ParkirajBa.Repositories
{
    public class RequestRepository : IRequestRepository
    {
        private readonly ApplicationDbContext _Database;

        public RequestRepository(ApplicationDbContext database)
        {
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

        public async Task<List<ParkingRequest>> GetAllAsync()
        {
            return await _Database.ParkingRequest.ToListAsync();
        }

        public async Task<ParkingRequest?> GetByIdAsync(int id)
        {
            return await _Database.ParkingRequest.FirstOrDefaultAsync(r => r.ID == id);
        }

        public async Task<ParkingRequest?> GetByParkingIdAsync(int parkingId)
        {
            return await _Database.ParkingRequest.FirstOrDefaultAsync(r => r.ParkingID == parkingId);
        }

        public async Task DeleteAsync(int id)
        {
            var request = await _Database.ParkingRequest.FindAsync(id);
            if (request != null)
            {
                _Database.ParkingRequest.Remove(request);
                await _Database.SaveChangesAsync();
            }
        }
    }
}