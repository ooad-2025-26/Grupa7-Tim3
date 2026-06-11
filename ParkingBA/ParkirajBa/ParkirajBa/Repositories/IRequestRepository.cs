using ParkirajBa.Models;

namespace ParkirajBa.Repositories
{
    public interface IRequestRepository
    {
        Task AddParkingRequestAsync(ParkingRequest Request);

        Task<ParkingRequest> AddParkingRequestAsync(int ParkingId);
    }
}
