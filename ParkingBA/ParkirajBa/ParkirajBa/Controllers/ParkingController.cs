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

        public ParkingController(
            IParkingRepository parkingRepository,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext database)
        {
            _parkingRepository = parkingRepository;
            _userManager = userManager;
            _database = database;
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

            return View(parking);
        }

        // GET: /Parking/ParkingManagement — Owner only
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> ParkingManagement()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            var parkingObjects = await _parkingRepository.GetByOwnerIdAsync(currentUser.Id);

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
        public async Task<IActionResult> Create(
            ParkingObject parkingObject,
            List<IFormFile> Images,
            List<int> ImagePositions,
            List<PricingCreateDto> Pricings)
        {
            try
            {
                // 1. Postavi OwnerId
                var owner = await _userManager.GetUserAsync(User);
                if (owner == null)
                    return Json(new { success = false, message = "Korisnik nije pronađen" });

                parkingObject.OwnerId = owner.Id;

                // 2. Validacija
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new
                    {
                        success = false,
                        message = $"Greške: {string.Join(", ", errors)}"
                    });
                }

                // 3. Postavi availableSpots
                parkingObject.availableSpots = parkingObject.totalSpots ?? 0;

                // 4. Spremi parking - AWAIT AKO JE ASYNC
                await _parkingRepository.AddParking(parkingObject);  // ← AWAIT!

                // 5. Spremi slike - NAKON parking operacije
                if (Images != null && Images.Count > 0)
                {
                    try
                    {
                        await _parkingRepository.SaveAllParkingImagesByIDAsync(Images, ImagePositions, parkingObject.ID);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Image save error: {ex.Message}");
                    }
                }

                // 6. Spremi cijene - NAKON slike
                if (Pricings != null && Pricings.Count > 0)
                {
                    foreach (var pricingDto in Pricings)
                    {
                        var pricing = new Pricing
                        {
                            pricingType = (PricingType)pricingDto.pricingType,
                            price = pricingDto.price,
                            ParkingObjectID = parkingObject.ID,
                            validFrom = string.IsNullOrEmpty(pricingDto.validFrom)
                                ? null
                                : (DateTime.TryParse(pricingDto.validFrom, out var date) ? (DateTime?)date : null)
                        };

                        await _parkingRepository.AddPricingAsync(pricing);
                    }
                }

                return Json(new
                {
                    success = true,
                    message = "Parking uspješno kreiran!"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Greška: {ex.Message}"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ParkingObject ChangedParking)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Podaci nisu validni.");
            }

            ParkingObject parking= await _parkingRepository.ModifyParkingAsync(ChangedParking);

            if (parking == null) return NotFound();

            return Json(new { success = true, message = "Izmjene su uspješno spašene!" });
        }

        //----



    }

}