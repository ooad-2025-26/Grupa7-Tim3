using ParkirajBa.Models;

namespace ParkirajBa.Repositories
{
    public interface IParkingRepository
    {
        public Task<List<ParkingObject>>
            FilterParkings(string searchText, bool hasGarage, bool hasEVCharger, bool hasCameras, bool isDisabledAccessible, string regime, int maxPrice);

        public Task<ParkingObject>
            AddParking(ParkingObject NewParking);
        
    }
}
