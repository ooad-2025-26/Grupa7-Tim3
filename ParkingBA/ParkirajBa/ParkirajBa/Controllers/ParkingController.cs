using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Repositories;

namespace ParkirajBa.Controllers
{
    public class ParkingController : Controller
    {
        private readonly IParkingRepository _parkingRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _database;
        private readonly IRequestRepository _requestRepository;

        public ParkingController(
            IParkingRepository parkingRepository,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext database,
            IRequestRepository requestRepository)
        {
            _parkingRepository = parkingRepository;
            _userManager = userManager;
            _database = database;
            _requestRepository = requestRepository;
        }

        // GET: /Parking/Details/5 — everyone
        public async Task<IActionResult> Details(int id)
        {
            var parking = await _parkingRepository.GetByIdAsync(id);

            if (parking == null)
            {
                Console.WriteLine("Id is " + id);
                return RedirectToAction("Objekti", "Home");
            }
            var pricings = await _parkingRepository.GetPricingsByParkingIdAsync(id);
            ViewBag.Pricings = pricings;
            ViewBag.PrimaryImage = await _parkingRepository.GetPrimaryImageByParkingIDAsync(id);
            return View(parking);
        }

        // GET: /Parking/ParkingManagement — Owner only
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> ParkingManagement()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var parkingObjects = await _parkingRepository.GetByOwnerIdAsync(currentUser.Id);

            var slike = new Dictionary<int, string>();
            foreach (var parking in parkingObjects)
            {
                var slika = await _parkingRepository.GetPrimaryImageByParkingIDAsync(parking.ID);
                slike[parking.ID] = slika?.ImagePath ?? "/images/parking.jpg";
            }
            ViewBag.Slike = slike;

            return View(parkingObjects);
        }
        /*
        // GET: /Parking/Edit/5 — Owner or Admin
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var parking = await _parkingRepository.GetByIdWithPricingsAsync(id);
            if (parking == null) return NotFound();

            if (User.IsInRole("Owner"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (parking.OwnerId != currentUser?.Id)
                    return Forbid();
            }

            return View(parking);
        }

        // POST: /Parking/Edit/5
        
        [HttpPost]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> Edit(int id, string name, string address,
            int totalSpots, bool hasCameras, bool isDisabledAccessible,
            bool hasEVCharger, bool isUnderground, double? maxHeight,
            TimeOnly? opensAt, TimeOnly? closesAt,
            List<int>? pricingIds, List<decimal>? pricingValues)
        {
            var parking = await _parkingRepository.GetByIdAsync(id);
            if (parking == null) return NotFound();

            // owner can edit only their own parking, admin can edit all
            if (User.IsInRole("Owner"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (parking.OwnerId != currentUser?.Id)
                    return Forbid();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                ViewBag.Error = "Naziv parkinga je obavezan.";
                return View(parking);
            }

            if (totalSpots < 1)
            {
                ViewBag.Error = "Ukupan broj mjesta mora biti veći od 0.";
                return View(parking);
            }

            // validate prices server-side
            if (pricingValues != null && pricingValues.Any(p => p < 0))
            {
                ViewBag.Error = "Cijena ne može biti negativna.";
                ViewBag.Pricings = await _parkingRepository.GetPricingsByParkingIdAsync(id);
                return View("~/Views/Admin/ParkingDetails.cshtml", parking);
            }

            int diff = totalSpots - (parking.totalSpots ?? 0);
            parking.name = name;
            parking.address = address;
            parking.totalSpots = totalSpots;
            parking.availableSpots = Math.Max(0, parking.availableSpots + diff);
            parking.hasCameras = hasCameras;
            parking.isDisabledAccessible = isDisabledAccessible;
            parking.hasEVCharger = hasEVCharger;
            parking.isUnderground = isUnderground;
            parking.maxHeight = maxHeight;

            // radno vrijeme — čuvamo kao DateTime s today datumom, samo je vrijeme bitno
            parking.opensAt = opensAt.HasValue
                ? (DateTime?)DateTime.Today.Add(opensAt.Value.ToTimeSpan())
                : null;
            parking.closesAt = closesAt.HasValue
                ? (DateTime?)DateTime.Today.Add(closesAt.Value.ToTimeSpan())
                : null;

            _database.ParkingObject.Update(parking);

            // update pricing rows
            if (pricingIds != null && pricingValues != null)
            {
                for (int i = 0; i < pricingIds.Count && i < pricingValues.Count; i++)
                {
                    var pricing = await _database.Pricing.FindAsync(pricingIds[i]);
                    if (pricing != null && pricing.ParkingObjectID == id)
                    {
                        pricing.price = pricingValues[i];
                        _database.Pricing.Update(pricing);
                    }
                }
            }

            await _database.SaveChangesAsync();

            TempData["Success"] = "Parking je uspješno ažuriran.";

            // redirect
            if (User.IsInRole("Admin"))
                return RedirectToAction("ParkingDetails", "Admin", new { id });

            return RedirectToAction("ParkingManagement");
        }
        */

        //-- My Objects - Create /Edit
        [HttpGet]
        [Authorize(Roles="Owner,Admin")]
        public IActionResult GetCreateForm()
        {
            // Vraća čistu kreiraj formu
            return PartialView("_ParkingCreate");
        }

        [HttpGet]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> GetEditForm(int id)
        {
            var parking = await _parkingRepository.GetByIdAsync(id);
            if (parking == null) return NotFound();

            // Vraća edit formu sa modelom
            return PartialView("_ParkingEdit", parking);
        }


        [HttpPost]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> Create(
            ParkingObject parkingObject,
            List<IFormFile> Images,
            List<int> ImagePositions,
            List<PricingCreateDto> Pricings)
        {
            try
            {
                var owner = await _userManager.GetUserAsync(User);
                if (owner == null)
                    return Json(new { success = false, message = "Korisnik nije pronađen." });

                // Backend validacija obaveznih polja
                if (string.IsNullOrWhiteSpace(parkingObject.name))
                    return Json(new { success = false, message = "Naziv parkinga je obavezan." });

                if (string.IsNullOrWhiteSpace(parkingObject.address))
                    return Json(new { success = false, message = "Adresa je obavezna." });

                if (!parkingObject.totalSpots.HasValue || parkingObject.totalSpots < 1)
                    return Json(new { success = false, message = "Ukupan broj mjesta mora biti veći od 0." });

                if (parkingObject.latitude == 0 || parkingObject.longitude == 0)
                    return Json(new { success = false, message = "Koordinate su obavezne." });

                parkingObject.OwnerId = owner.Id;
                parkingObject.availableSpots = parkingObject.totalSpots ?? 0;
                parkingObject.Pricings.Clear(); // spriječi duplo snimanje 

                parkingObject.isApproved = false;

                await _parkingRepository.AddParking(parkingObject);

                if (Images != null && Images.Count > 0)
                {
                    try { await _parkingRepository.SaveAllParkingImagesByIDAsync(Images, ImagePositions, parkingObject.ID); }
                    catch (Exception ex) { Console.WriteLine($"Greška pri snimanju slike: {ex.Message}"); }
                }

                if (Pricings != null && Pricings.Count > 0)
                {
                    foreach (var pricingDto in Pricings)
                    {
                        if (pricingDto.price < 0)
                            return Json(new { success = false, message = "Cijena ne može biti negativna." });

                        var pricing = new Pricing
                        {
                            pricingType = (PricingType)pricingDto.pricingType,
                            price = pricingDto.price,
                            ParkingObjectID = parkingObject.ID,
                            validFrom = string.IsNullOrEmpty(pricingDto.validFrom) ? null
                                : (DateTime.TryParse(pricingDto.validFrom, out var date) ? (DateTime?)date : null)
                        };
                        await _parkingRepository.AddPricingAsync(pricing);
                    }
                }

                //Sending approval request to admin
                await _requestRepository.AddParkingRequestAsync(parkingObject.ID);

                return Json(new { success = true, message = "Parking uspješno kreiran!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Greška pri kreiranju parkinga: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> UploadCoverImage(int id, IFormFile Images)
        {
            // Provjera vlasništva za Owner
            if (User.IsInRole("Owner"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var parking = await _parkingRepository.GetByIdAsync(id);
                if (parking == null)
                    return Json(new { success = false, message = "Parking nije pronađen." });
                if (parking.OwnerId != currentUser?.Id)
                    return Json(new { success = false, message = "Nemate pravo mijenjati ovu sliku." });
            }

            if (Images == null || Images.Length == 0)
                return Json(new { success = false, message = "Molimo odaberite sliku." });

            // Obriši staru sliku
            var postojece = await _database.ParkingImages
                .Where(i => i.ParkingObjectID == id && i.Position == 1)
                .ToListAsync();

            foreach (var stara in postojece)
            {
                if (!string.IsNullOrEmpty(stara.ImagePath))
                {
                    var fizickaPutanja = Path.Combine(
                        Directory.GetCurrentDirectory(), "wwwroot",
                        stara.ImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(fizickaPutanja))
                        System.IO.File.Delete(fizickaPutanja);
                }
            }

            _database.ParkingImages.RemoveRange(postojece);
            await _database.SaveChangesAsync();

            // Spremi novu
            await _parkingRepository.SaveParkingImageByIDAsync(Images, 1, id);

            return Json(new { success = true, message = "Slika uspješno ažurirana." });
        }

        [HttpPost]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> Edit(
            int id, string name, string address,
            string latitude, string longitude,
            int? totalSpots, double? maxHeight,
            bool hasCameras, bool isDisabledAccessible,
            bool hasEVCharger, bool isUnderground,
            string? opensAt, string? closesAt,
            List<PricingCreateDto>? Pricings,
            List<IFormFile>? Images,        
            List<int>? ImagePositions,
             List<int>? pricingIds,        
            List<decimal>? pricingValues) 

        {
            var parking = await _parkingRepository.GetByIdAsync(id);
            if (parking == null) return Json(new { success = false, message = "Parking nije pronađen." });
            if (!double.TryParse(latitude, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double lat))
                return Json(new { success = false, message = "Neispravan format geografske širine." });

            if (!double.TryParse(longitude, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double lon))
                return Json(new { success = false, message = "Neispravan format geografske dužine." });
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Naziv parkinga je obavezan." });

            if (!totalSpots.HasValue || totalSpots < 1)
                return Json(new { success = false, message = "Ukupan broj mjesta mora biti veći od 0." });

            int diff = totalSpots.Value - (parking.totalSpots ?? 0);
            parking.name = name;
            parking.address = address;
            parking.latitude = lat;
            parking.longitude = lon;
            parking.totalSpots = totalSpots;
            parking.availableSpots = Math.Max(0, parking.availableSpots + diff);
            parking.maxHeight = maxHeight;
            parking.hasCameras = hasCameras;
            parking.isDisabledAccessible = isDisabledAccessible;
            parking.hasEVCharger = hasEVCharger;
            parking.isUnderground = isUnderground;
            parking.opensAt = !string.IsNullOrEmpty(opensAt)
       ? DateTime.Parse(opensAt)
       : null;
            parking.closesAt = !string.IsNullOrEmpty(closesAt)
                ? DateTime.Parse(closesAt)
                : null;

            await _parkingRepository.ModifyParkingAsync(parking);
            if ((Pricings == null || Pricings.Count == 0) && pricingIds != null && pricingValues != null)
            {
                for (int i = 0; i < pricingIds.Count && i < pricingValues.Count; i++)
                {
                    var pricing = await _database.Pricing.FindAsync(pricingIds[i]);
                    if (pricing != null && pricing.ParkingObjectID == id)
                    {
                        pricing.price = pricingValues[i];
                        _database.Pricing.Update(pricing);
                    }
                }
                await _database.SaveChangesAsync();
            }
            if (Images != null && Images.Count > 0)
            {
                try
                {
                    await _parkingRepository.SaveAllParkingImagesByIDAsync(Images, ImagePositions ?? new List<int>(), id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greška pri snimanju slike: {ex.Message}");
                }
            }

            if (Pricings != null && Pricings.Count > 0)
            {
                var existing = await _parkingRepository.GetPricingsByParkingIdAsync(id);

                foreach (var pricingDto in Pricings)
                {
                    if (pricingDto.price < 0) continue;

                    var tip = (PricingType)pricingDto.pricingType;
                    var postojeca = existing.FirstOrDefault(p => p.pricingType == tip);

                    if (postojeca != null)
                    {
                        postojeca.price = pricingDto.price;
                        if (!string.IsNullOrEmpty(pricingDto.validFrom) &&
                            DateTime.TryParse(pricingDto.validFrom, out var date))
                            postojeca.validFrom = date;

                        _database.Pricing.Update(postojeca);
                    }
                    else
                    {
                        var novaCijena = new Pricing
                        {
                            pricingType = tip,
                            price = pricingDto.price,
                            ParkingObjectID = id,
                            validFrom = string.IsNullOrEmpty(pricingDto.validFrom) ? null
                                : (DateTime.TryParse(pricingDto.validFrom, out var d) ? (DateTime?)d : null)
                        };
                        await _parkingRepository.AddPricingAsync(novaCijena);
                    }
                }

                await _database.SaveChangesAsync();
            }

            if (User.IsInRole("Admin"))
            {
                TempData["Success"] = "Parking uspješno ažuriran!";
                return RedirectToAction("ParkingDetails", "Admin", new { id });
            }

            return Json(new { success = true, message = "Parking uspješno ažuriran!" });
        }



    }

}