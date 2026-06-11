using ParkirajBa.Models;

namespace ParkirajBa.Repositories
{
    public interface IRequestRepository
    {
        Task AddParkingRequestAsync(ParkingRequest Request);
        Task<ParkingRequest> AddParkingRequestAsync(int ParkingId);

        Task<List<ParkingRequest>> GetAllAsync();
        Task<ParkingRequest?> GetByIdAsync(int id);
        Task<ParkingRequest?> GetByParkingIdAsync(int parkingId);
        Task DeleteAsync(int id);
    }
}