using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Services;

namespace ParkirajBa.Repositories
{
    public class ParkingRepository : IParkingRepository
    {
        private readonly ApplicationDbContext _Database;
        private readonly ImageService _ImageService;

        public ParkingRepository(ApplicationDbContext database, ImageService imageService)
        {
            _Database = database;
            _ImageService = imageService;
        }

        // -- Filtering and searching --

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


        //----

        //-- Parking --
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

        public async Task<ParkingObject?> ModifyParkingAsync(ParkingObject ChangedParking)
        {
            var DatabaseParking = await _Database.ParkingObject.FindAsync(ChangedParking.ID);
            if (DatabaseParking == null) return null;

            
            DatabaseParking.name = ChangedParking.name;
            DatabaseParking.address = ChangedParking.address;
            DatabaseParking.latitude = ChangedParking.latitude;
            DatabaseParking.longitude = ChangedParking.longitude;
            DatabaseParking.totalSpots = ChangedParking.totalSpots;
            DatabaseParking.maxHeight = ChangedParking.maxHeight;
            DatabaseParking.hasCameras = ChangedParking.hasCameras;
            DatabaseParking.hasEVCharger = ChangedParking.hasEVCharger;
            DatabaseParking.isDisabledAccessible = ChangedParking.isDisabledAccessible;
            DatabaseParking.isUnderground = ChangedParking.isUnderground;
            DatabaseParking.opensAt = ChangedParking.opensAt;
            DatabaseParking.closesAt = ChangedParking.closesAt;
            _Database.SaveChanges();
            return DatabaseParking;
        }
        //----

        // -- Parking image --
        public async Task<List<string?>> GetImagePathsByParkingIDAsync(int parkingID)
        {
            var images = await _Database.ParkingImages.Where(i => i.ParkingObjectID == parkingID).OrderBy(i=>i.Position).ToListAsync();
            List<string?> paths=new List<string?>();
            foreach (var image in images)
            {
                paths.Add(image.ImagePath);
            }
            return paths;
        }

        public async Task<List<ParkingImage?>?> GetImagesByParkingIDAsync(int parkingID)
        {
            return await _Database.ParkingImages.Where(i=> i.ParkingObjectID== parkingID).OrderBy(i=>i.Position).ToListAsync();
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

        public async Task<string> SaveParkingImageByIDAsync(IFormFile Image, int Position, int ParkingID)
        {
            string path = await _ImageService.SaveImageToServerAsync(Image,"images/Parkings",$"ForParkingID{ParkingID}");
            ParkingImage img= new ParkingImage { ImagePath= path , ParkingObjectID=ParkingID, Position=Position};

            _Database.ParkingImages.Add(img);
            await _Database.SaveChangesAsync();

            return img.ImagePath;
        }
        //can be optimized by not using the SaveParkingImageByIDAsync because it saves changes after every image
        public async Task<List<string>> SaveAllParkingImagesByIDAsync(List<IFormFile> Images, List<int> Positions, int ParkingID)
        {
            if (Images.Count != Positions.Count)
                throw new Exception("Dimensions of Images and their Positions dont align");

            List<string> paths = new List<string>();
            for(int i = 0; i < Images.Count; i++)
            {
                paths.Add(await SaveParkingImageByIDAsync(Images[i], Positions[i], ParkingID));
            }

            return paths;
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

        public async Task<Pricing?> AddPricingAsync(Pricing pricing)
        {

            _Database.Pricing.Add(pricing);
            await _Database.SaveChangesAsync();
            return pricing;
        }

        public async Task<List<Pricing?>?> AddAllPricingsAsync(List<Pricing> Pricings)
        {
            foreach (var pricing in Pricings)
            {
                await AddPricingAsync(pricing);
            }

            return Pricings;
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