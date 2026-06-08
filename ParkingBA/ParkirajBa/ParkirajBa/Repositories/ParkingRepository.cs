using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;

namespace ParkirajBa.Repositories
{
    public class ParkingRepository : IParkingRepository
    {
        private readonly ApplicationDbContext _Database;

        public ParkingRepository(ApplicationDbContext database)
        {
            _Database = database;
        }

        // ── Filtering and searching

        public async Task<List<ParkingObject>> FilterParkings(
            string searchText, bool hasGarage, bool hasEVCharger,
            bool hasCameras, bool isDisabledAccessible, string regime, int maxPrice)
        {
            var query = _Database.ParkingObject.AsQueryable();

            if (!string.IsNullOrEmpty(searchText))
                query = query.Where(p => p.name.Contains(searchText));

            if (hasGarage)
                query = query.Where(p => p.isUnderground ?? false);

            if (hasEVCharger)
                query = query.Where(p => p.hasEVCharger ?? false);

            if (hasCameras)
                query = query.Where(p => p.hasCameras ?? false);

            if (isDisabledAccessible)
                query = query.Where(p => p.isDisabledAccessible ?? false);

            PricingType typeByRegime = regime switch
            {
                "Day" => PricingType.Daily,
                "Week" => PricingType.Weekly,
                "Month" => PricingType.Monthly,
                "Year" => PricingType.Yearly,
                _ => PricingType.Hourly
            };

            query = query.Where(p =>
                _Database.Pricing.Any(pricing =>
                    pricing.ParkingObjectID == p.ID &&
                    pricing.pricingType == typeByRegime &&
                    pricing.price < maxPrice));

            return await query.ToListAsync();
        }

        public async Task<ParkingObject> AddParking(ParkingObject newParking)
        {
            _Database.ParkingObject.Add(newParking);
            await _Database.SaveChangesAsync();
            return newParking;
        }

      
        public async Task<List<ParkingObject>> GetAllAsync()
        {
            return await _Database.ParkingObject
                .ToListAsync();
        }

        public async Task<List<ParkingObject>> GetAllWithPricingsAsync()
        {
            return await _Database.ParkingObject
                .Include(p => p.Pricings)
                .ToListAsync();
        }

        public async Task<List<ParkingObject>> GetAllWithOwnerAsync()
        {
            return await _Database.ParkingObject
                .Include(p => p.Owner)
                .ToListAsync();
        }

        public async Task<List<ParkingObject>> GetByOwnerIdAsync(string ownerId)
        {
            return await _Database.ParkingObject
                .Where(p => p.OwnerId == ownerId)
                .ToListAsync();
        }

        public async Task<List<ParkingObject>> GetByOwnerIdWithPricingsAsync(string ownerId)
        {
            return await _Database.ParkingObject
                .Where(p => p.OwnerId == ownerId)
                .Include(p => p.Pricings)
                .ToListAsync();
        }

        public async Task<ParkingObject?> GetByIdAsync(int id)
        {
            return await _Database.ParkingObject
                .FirstOrDefaultAsync(p => p.ID == id);
        }

        public async Task<ParkingObject?> GetByIdWithPricingsAsync(int id)
        {
            return await _Database.ParkingObject
                .Include(p => p.Pricings)
                .FirstOrDefaultAsync(p => p.ID == id);
        }

        public async Task<ParkingObject?> GetByIdWithOwnerAsync(int id)
        {
            return await _Database.ParkingObject
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.ID == id);
        }

        // --Parking image getters
        public async Task<List<string?>> GetImagePathsByParkingIDAsync(int parkingID)
        {
            var images = await _Database.ParkingImages.Where(i => i.ParkingObjectID == parkingID).ToListAsync();
            List<string?> paths=new List<string?>();
            foreach (var image in images)
            {
                paths.Add(image.ImagePath);
            }
            return paths;
        }

        public async Task<List<ParkingImage?>?> GetImagesByParkingIDAsync(int parkingID)
        {
            return await _Database.ParkingImages.Where(i=> i.ParkingObjectID== parkingID).ToListAsync();
        }

        public async Task<string?> GetPrimaryImagePathByParkingIDAsync(int parkingID)
        {
            ParkingImage img= await GetPrimaryImageByParkingIDAsync(parkingID);
            return img.ImagePath;

        }
        public async Task<ParkingImage?> GetPrimaryImageByParkingIDAsync(int parkingID)
        {
            return await _Database.ParkingImages.Where(i => i.ParkingObjectID == parkingID).OrderBy(i => i.Position).FirstOrDefaultAsync();
        }

        // ── Prices

        public async Task<List<Pricing>> GetPricingsByParkingIdAsync(int parkingObjectId)
        {
            return await _Database.Pricing
                .Where(p => p.ParkingObjectID == parkingObjectId)
                .ToListAsync();
        }

        public async Task<Pricing?> GetActivePricingAsync(int parkingObjectId, PricingType type)
        {
            var now = DateTime.Now;

            return await _Database.Pricing
                .Where(p =>
                    p.ParkingObjectID == parkingObjectId &&
                    p.pricingType == type &&
                    (p.validFrom == null || p.validFrom <= now) &&
                    (p.validTo == null || p.validTo >= now))
                .OrderByDescending(p => p.validFrom)
                .FirstOrDefaultAsync();
        }

        // ── Tiket

        public async Task<List<Ticket>> GetTicketsByUserIdAsync(string userId)
        {
            return await _Database.Tickets
                .Include(t => t.ParkingObject)
                .Where(t => t.ApplicationUserId == userId)
                .OrderByDescending(t => t.IssuedAt)
                .ToListAsync();
        }

        public async Task<List<Ticket>> GetTicketsByParkingIdsAsync(List<int> parkingIds)
        {
            return await _Database.Tickets
                .Include(t => t.ParkingObject)
                .Where(t => parkingIds.Contains(t.ParkingObjectId))
                .ToListAsync();
        }

        public async Task<Ticket?> GetTicketByIdAsync(int id, string userId)
        {
            return await _Database.Tickets
                .Include(t => t.ParkingObject)
                .FirstOrDefaultAsync(t => t.Id == id && t.ApplicationUserId == userId);
        }

        public async Task<List<Pricing>>
            GetParkingPricings(int ParkingObjectID)
        {
            return await _Database.Pricing.Where(p => p.ParkingObjectID == ParkingObjectID).ToListAsync();
        }
    }
}