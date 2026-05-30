using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;

namespace ParkirajBa.Repositories
{
    public class ParkingRepository:IParkingRepository
    {
        ApplicationDbContext _Database;

        public ParkingRepository(ApplicationDbContext database)
        {
            this._Database = database;
        }

        public async Task<List<ParkingObject>>
            FilterParkings(string searchText, bool hasGarage, bool hasEVCharger, bool hasCameras, bool isDisabledAccessible, string regime, int maxPrice)
        {
            var query = _Database.ParkingObject.AsQueryable();
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(p => p.name.Contains(searchText));
            }

            if (hasGarage)
            {
                query = query.Where(p => p.isUnderground ?? false);
            }

            if (hasEVCharger)
            {
                query = query.Where(p => p.hasEVCharger ?? false);
            }

            if (hasCameras)
            {
                query = query.Where(p => p.hasCameras ?? false);
            }

            if (isDisabledAccessible)
            {
                query = query.Where(p => p.isDisabledAccessible ?? false);
            }

            PricingType typeByRegime = PricingType.Hourly;
            if (regime.Equals("Hour"))
                typeByRegime = PricingType.Hourly;
            else if (regime.Equals("Day"))
                typeByRegime = PricingType.Daily;
            else if (regime.Equals("Week"))
                typeByRegime = PricingType.Weekly;
            else if (regime.Equals("Month"))
                typeByRegime = PricingType.Monthly;
            else if (regime.Equals("Year"))
                typeByRegime = PricingType.Yearly;


            // Filter by maximum price
            query = query.Where(p => _Database.Pricing.Any(pricing => pricing.ParkingObjectID == p.ID && pricing.pricingType == typeByRegime && pricing.price < maxPrice));

            // Execute query and fetch data
            return await query.ToListAsync();

        }

        public async Task<ParkingObject>
            AddParking(ParkingObject NewParking)
        {
            _Database.ParkingObject.Add(NewParking);
            
            await _Database.SaveChangesAsync();

            return NewParking;
        }

        public async Task<List<Pricing>>
            GetParkingPricings(int ParkingObjectID)
        {
            return await _Database.Pricing.Where(p => p.ParkingObjectID == ParkingObjectID).ToListAsync();
        }
    }
}
