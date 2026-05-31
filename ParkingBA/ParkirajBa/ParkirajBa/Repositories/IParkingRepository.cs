using ParkirajBa.Models;

namespace ParkirajBa.Repositories
{
    public interface IParkingRepository
    {
        // ── Filtering and searching
        Task<List<ParkingObject>> FilterParkings(string searchText, bool hasGarage, bool hasEVCharger, bool hasCameras, bool isDisabledAccessible, string regime, int maxPrice);
        Task<ParkingObject> AddParking(ParkingObject newParking);

        // ── Getting parkings and related data
        Task<List<ParkingObject>> GetAllAsync();
        Task<List<ParkingObject>> GetAllWithPricingsAsync();
        Task<List<ParkingObject>> GetAllWithOwnerAsync();
        Task<List<ParkingObject>> GetByOwnerIdAsync(string ownerId);
        Task<List<ParkingObject>> GetByOwnerIdWithPricingsAsync(string ownerId);
        Task<ParkingObject?> GetByIdAsync(int id);
        Task<ParkingObject?> GetByIdWithPricingsAsync(int id);
        Task<ParkingObject?> GetByIdWithOwnerAsync(int id);

        // ── Prices
        Task<List<Pricing>> GetPricingsByParkingIdAsync(int parkingObjectId);
        Task<Pricing?> GetActivePricingAsync(int parkingObjectId, PricingType type);

        // ── Ticket
        Task<List<Ticket>> GetTicketsByUserIdAsync(string userId);
        Task<List<Ticket>> GetTicketsByParkingIdsAsync(List<int> parkingIds);
        Task<Ticket?> GetTicketByIdAsync(int id, string userId);
    }
}