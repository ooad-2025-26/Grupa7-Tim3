using ParkirajBa.Models;

namespace ParkirajBa.Repositories
{
    public interface IParkingRepository
    {
        // ── Filtering and searching
        Task<List<ParkingObject>> FilterParkings
            (string searchText, bool hasGarage, bool hasEVCharger, bool hasCameras, bool isDisabledAccessible, string regime, int maxPrice);
        
        Task<List<ParkingObject>> FilterApprovedParkings
            (string searchText, bool hasGarage, bool hasEVCharger, bool hasCameras, bool isDisabledAccessible, string regime, int maxPrice);

        // ── Getting parkings and related data
        Task<ParkingObject> AddParking(ParkingObject newParking);
        Task<List<ParkingObject>> GetAllAsync();
        Task<List<ParkingObject>> GetAllApprovedAsync();
        Task<bool> IncrementAvailableSpotsAsync(int parkingId);
        Task<List<ParkingObject>> GetAllWithPricingsAsync();
        Task<List<ParkingObject>> GetAllWithOwnerAsync();
        Task<List<ParkingObject>> GetByOwnerIdAsync(string ownerId);
        Task<List<ParkingObject>> GetByOwnerIdWithPricingsAsync(string ownerId);
        Task<ParkingObject?> GetByIdAsync(int id);
        Task<ParkingObject?> GetByIdWithPricingsAsync(int id);
        Task<ParkingObject?> GetByIdWithOwnerAsync(int id);

        Task<ParkingObject?> ModifyParkingAsync(ParkingObject ChangedParking);



        //--Parking Images
        Task<List<string?>> GetImagePathsByParkingIDAsync(int parkingID);
        Task<List<ParkingImage?>?> GetImagesByParkingIDAsync(int parkingID);
        Task<string?> GetPrimaryImagePathByParkingIDAsync(int parkingID);
        Task<ParkingImage?> GetPrimaryImageByParkingIDAsync(int parkingID);

        Task<string> SaveParkingImageByIDAsync(IFormFile Image, int Position, int ParkingID);

        Task<List<string>> SaveAllParkingImagesByIDAsync(List<IFormFile> Images, List<int> Positions, int ParkingID);

        // ── Prices
        Task<List<Pricing>> GetPricingsByParkingIdAsync(int parkingObjectId);
        Task<Pricing?> GetActivePricingAsync(int parkingObjectId, PricingType type);
        Task<Pricing?> AddPricingAsync(Pricing pricing);
        Task<List<Pricing?>?> AddAllPricingsAsync(List<Pricing> Pricings);

        // ── Ticket
        Task<List<Ticket>> GetTicketsByUserIdAsync(string userId);
        Task<List<Ticket>> GetTicketsByParkingIdsAsync(List<int> parkingIds);
        Task<Ticket?> GetTicketByIdAsync(int id, string userId);

    }
}