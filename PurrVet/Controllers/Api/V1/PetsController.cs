using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PurrVet.DTOs.Appointments;
using PurrVet.DTOs.Common;
using PurrVet.DTOs.PetCards;
using PurrVet.DTOs.Pets;
using PurrVet.Infrastructure;
using PurrVet.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Globalization;

namespace PurrVet.Controllers.Api.V1 {
    [ApiController]
    [Route("api/v1/pets")]
    [Authorize(Policy = "OwnerOnly")]
    [Tags("Pets")]
    public class PetsController : ControllerBase {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public PetsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment) {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        private string? BuildPhotoUrl(string? photoPath) {
            if (string.IsNullOrEmpty(photoPath)) return null;
            return $"{Request.Scheme}://{Request.Host}{photoPath}";
        }

        private static string ComputeAge(DateTime birthdate) {
            int months = (int)Math.Floor((DateTime.Now - birthdate).TotalDays / 30.4375);
            return months >= 12 ? $"{months / 12} year(s) old" : $"{months} month(s) old";
        }

        [HttpGet]
        [EndpointSummary("List all pets")]
        [EndpointDescription("Returns all pets belonging to the authenticated owner, ordered by most recently added.")]
        [ProducesResponseType(typeof(ApiResponse<List<PetListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        public IActionResult GetPets() {
            var ownerId = User.GetOwnerId();

            var pets = _context.Pets
                .Where(p => p.OwnerID == ownerId)
                .OrderByDescending(p => p.PetID)
                .Select(p => new PetListItemDto {
                    PetId = p.PetID,
                    Name = p.Name,
                    Type = p.Type,
                    Breed = p.Breed,
                    Birthdate = p.Birthdate,
                    PhotoUrl = p.PhotoPath,
                    Age = ComputeAge(p.Birthdate),
                    CreatedAt = p.CreatedAt
                }).ToList();

            // Resolve photo URLs after materialization
            foreach (var p in pets)
                p.PhotoUrl = BuildPhotoUrl(p.PhotoUrl);

            return Ok(new ApiResponse<List<PetListItemDto>> { Success = true, Data = pets });
        }

        [HttpGet("{id}")]
        [EndpointSummary("Get pet details")]
        [EndpointDescription("Returns detailed information for a specific pet including paginated appointment history. Supports filtering by service category and text search on notes.")]
        [ProducesResponseType(typeof(ApiResponse<PetDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public IActionResult GetPet(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 5,
            [FromQuery] string? search = null, [FromQuery] int? categoryFilter = null) {
            var ownerId = User.GetOwnerId();

            var pet = _context.Pets
                .Include(p => p.Owner)
                .FirstOrDefault(p => p.PetID == id);

            if (pet == null)
                return NotFound(new ApiErrorResponse { Message = "Pet not found." });

            if (pet.OwnerID != ownerId)
                return StatusCode(403, new ApiErrorResponse { Message = "You do not have access to this pet." });

            var query = _context.Appointments
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .Where(a => a.PetID == id);

            if (categoryFilter.HasValue)
                query = query.Where(a => a.CategoryID == categoryFilter.Value);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(a => a.Notes != null && a.Notes.Contains(search));

            var total = query.Count();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var appointments = query
                .OrderByDescending(a => a.AppointmentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AppointmentListItemDto {
                    AppointmentId = a.AppointmentID,
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status,
                    GroupId = a.GroupID,
                    Notes = a.Notes,
                    PetId = a.PetID,
                    PetName = pet.Name,
                    ServiceType = a.ServiceCategory != null ? a.ServiceCategory.ServiceType : null,
                    ServiceSubtype = a.ServiceSubtype != null ? a.ServiceSubtype.ServiceSubType : null,
                    IsOwnAppointment = true
                }).ToList();

            return Ok(new ApiResponse<PetDetailDto> {
                Success = true,
                Data = new PetDetailDto {
                    PetId = pet.PetID,
                    Name = pet.Name,
                    Type = pet.Type,
                    Breed = pet.Breed,
                    Birthdate = pet.Birthdate,
                    PhotoUrl = BuildPhotoUrl(pet.PhotoPath),
                    Age = ComputeAge(pet.Birthdate),
                    OwnerName = pet.Owner.Name
                }
            });
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [EndpointSummary("Add a new pet")]
        [EndpointDescription("Create a new pet for the authenticated owner. Accepts an optional photo that will be cropped to 500x500 JPEG.")]
        [ProducesResponseType(typeof(ApiResponse<PetListItemDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePet([FromForm] CreatePetRequest request) {
            var ownerId = User.GetOwnerId();
            var userName = User.GetUserName();

            string? photoPath = null;
            if (request.Photo != null && request.Photo.Length > 0) {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pets");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}.jpg";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new MemoryStream()) {
                    await request.Photo.CopyToAsync(stream);
                    using (var original = System.Drawing.Image.FromStream(stream)) {
                        int side = Math.Min(original.Width, original.Height);
                        var cropRect = new System.Drawing.Rectangle(
                            (original.Width - side) / 2,
                            (original.Height - side) / 2,
                            side, side);

                        using (var cropped = new System.Drawing.Bitmap(500, 500)) {
                            using (var g = System.Drawing.Graphics.FromImage(cropped)) {
                                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                g.DrawImage(original,
                                    new System.Drawing.Rectangle(0, 0, 500, 500),
                                    cropRect,
                                    System.Drawing.GraphicsUnit.Pixel);
                            }
                            cropped.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                        }
                    }
                }
                photoPath = $"/uploads/pets/{fileName}";
            }

            var pet = new Pet {
                OwnerID = ownerId,
                Name = request.Name,
                Type = request.Type,
                Breed = string.IsNullOrWhiteSpace(request.Breed) ? "N/A" : request.Breed,
                Birthdate = request.Birthdate,
                PhotoPath = photoPath
            };

            _context.Pets.Add(pet);
            _context.Notifications.Add(new Notification {
                Message = $"A new pet '{pet.Name}' has been added by Owner ID:{ownerId}.",
                Type = "Pet",
                TargetRole = "Staff"
            });
            _context.SystemLogs.Add(new SystemLog {
                ActionType = "Create",
                Module = "Pet",
                Description = $"Added a Pet: {pet.Name} (via mobile)",
                PerformedBy = userName,
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return StatusCode(201, new ApiResponse<PetListItemDto> {
                Success = true,
                Message = "Pet added successfully!",
                Data = new PetListItemDto {
                    PetId = pet.PetID,
                    Name = pet.Name,
                    Type = pet.Type,
                    Breed = pet.Breed,
                    Birthdate = pet.Birthdate,
                    PhotoUrl = BuildPhotoUrl(pet.PhotoPath),
                    Age = ComputeAge(pet.Birthdate),
                    CreatedAt = pet.CreatedAt
                }
            });
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        [EndpointSummary("Update a pet")]
        [EndpointDescription("Update pet information. All fields are optional — only provided fields are updated. A new photo replaces the existing one.")]
        [ProducesResponseType(typeof(ApiResponse<PetListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePet(int id, [FromForm] UpdatePetRequest request) {
            var ownerId = User.GetOwnerId();
            var userName = User.GetUserName();

            var pet = _context.Pets.FirstOrDefault(p => p.PetID == id && p.OwnerID == ownerId);
            if (pet == null)
                return NotFound(new ApiErrorResponse { Message = "Pet not found." });

            if (!string.IsNullOrWhiteSpace(request.Name)) pet.Name = request.Name.Trim();
            if (!string.IsNullOrWhiteSpace(request.Type)) pet.Type = request.Type.Trim();
            if (request.Breed != null) pet.Breed = string.IsNullOrWhiteSpace(request.Breed) ? "N/A" : request.Breed.Trim();
            if (request.Birthdate.HasValue) pet.Birthdate = request.Birthdate.Value;

            if (request.Photo != null && request.Photo.Length > 0) {
                if (!string.IsNullOrEmpty(pet.PhotoPath)) {
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", pet.PhotoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) {
                        try { System.IO.File.Delete(oldPath); } catch { }
                    }
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "pets");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.Photo.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await request.Photo.CopyToAsync(stream);

                pet.PhotoPath = $"/uploads/pets/{fileName}";
            }

            _context.Pets.Update(pet);
            _context.SystemLogs.Add(new SystemLog {
                ActionType = "Update",
                Module = "Pet",
                Description = $"Updated pet: {pet.PetID} - {pet.Name} (via mobile)",
                PerformedBy = userName,
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<PetListItemDto> {
                Success = true,
                Message = "Pet updated successfully!",
                Data = new PetListItemDto {
                    PetId = pet.PetID,
                    Name = pet.Name,
                    Type = pet.Type,
                    Breed = pet.Breed,
                    Birthdate = pet.Birthdate,
                    PhotoUrl = BuildPhotoUrl(pet.PhotoPath),
                    Age = ComputeAge(pet.Birthdate),
                    CreatedAt = pet.CreatedAt
                }
            });
        }

        [HttpGet("breeds")]
        [EndpointSummary("Get breed list")]
        [EndpointDescription("Returns a sorted list of breed names for the given pet type (`dog` or `cat`), sourced from the clinic's breed dataset.")]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public IActionResult GetBreeds([FromQuery] string type) {
            if (string.IsNullOrWhiteSpace(type))
                return BadRequest(new ApiErrorResponse { Message = "Type parameter is required (dog or cat)." });

            string filePath = Path.Combine(_hostEnvironment.ContentRootPath, "App_Data",
                type.ToLower() == "dog" ? "dogs_dataset.csv" : "cats_dataset.csv");

            if (!System.IO.File.Exists(filePath))
                return Ok(new ApiResponse<List<string>> { Success = true, Data = new List<string>() });

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var breeds = csv.GetRecords<dynamic>()
                .Select(r => (r as IDictionary<string, object>)?["Breed"]?.ToString())
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            return Ok(new ApiResponse<List<string?>> { Success = true, Data = breeds });
        }

        [HttpGet("{id}/card")]
        [EndpointSummary("Get pet health card")]
        [EndpointDescription("Returns the pet's health card with paginated completed appointment records, owner contact info, and age in months.")]
        [ProducesResponseType(typeof(ApiResponse<PetCardResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public IActionResult GetPetCard(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 12) {
            var ownerId = User.GetOwnerId();

            var pet = _context.Pets
                .Include(p => p.Owner)
                .Include(p => p.Appointments).ThenInclude(a => a.ServiceCategory)
                .Include(p => p.Appointments).ThenInclude(a => a.ServiceSubtype)
                .FirstOrDefault(p => p.PetID == id && p.OwnerID == ownerId);

            if (pet == null)
                return NotFound(new ApiErrorResponse { Message = "Pet not found." });

            var completed = pet.Appointments
                .Where(a => a.Status == "Completed")
                .OrderByDescending(a => a.AppointmentDate)
                .ToList();

            int totalPages = (int)Math.Ceiling((double)completed.Count / pageSize);
            var pageData = completed.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            int ageMonths = (int)Math.Floor((DateTime.Now - pet.Birthdate).TotalDays / 30.4375);

            var records = pageData.Select(a => new PetCardRecordDto {
                AppointmentId = a.AppointmentID,
                AppointmentDate = a.AppointmentDate,
                Notes = a.Notes,
                ServiceType = a.ServiceCategory?.ServiceType,
                ServiceSubtype = a.ServiceSubtype?.ServiceSubType,
                AdministeredBy = a.AdministeredBy,
                DueDate = a.DueDate
            }).ToList();

            return Ok(new ApiResponse<PetCardResponse> {
                Success = true,
                Data = new PetCardResponse {
                    Pet = new PetListItemDto {
                        PetId = pet.PetID,
                        Name = pet.Name,
                        Type = pet.Type,
                        Breed = pet.Breed,
                        Birthdate = pet.Birthdate,
                        PhotoUrl = BuildPhotoUrl(pet.PhotoPath),
                        Age = ComputeAge(pet.Birthdate),
                        CreatedAt = pet.CreatedAt
                    },
                    OwnerName = pet.Owner.Name,
                    OwnerPhone = pet.Owner.Phone,
                    OwnerEmail = pet.Owner.Email,
                    AgeInMonths = ageMonths,
                    Records = records,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalRecords = completed.Count
                }
            });
        }

        [HttpGet("{id}/card/pdf")]
        [EndpointSummary("Download pet card PDF")]
        [EndpointDescription("Generate and download an A5 PDF of the pet's vaccination and deworming health card.")]
        [Produces("application/pdf")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public IActionResult DownloadPetCardPdf(int id) {
            var ownerId = User.GetOwnerId();

            var pet = _context.Pets
                .Include(p => p.Owner)
                .Include(p => p.Appointments).ThenInclude(a => a.ServiceCategory)
                .Include(p => p.Appointments).ThenInclude(a => a.ServiceSubtype)
                .FirstOrDefault(p => p.PetID == id && p.OwnerID == ownerId);

            if (pet == null)
                return NotFound(new ApiErrorResponse { Message = "Pet not found." });

            var records = pet.Appointments
                .Where(a => a.Status == "Completed" &&
                       (a.ServiceCategory?.ServiceType == "Vaccination" ||
                        a.ServiceCategory?.ServiceType == "Deworming & Preventives"))
                .OrderBy(a => a.AppointmentDate)
                .ToList();

            string photoPath = System.IO.File.Exists($"wwwroot{pet.PhotoPath}")
                ? $"wwwroot{pet.PhotoPath}"
                : "wwwroot/uploads/profiles/pet.png";

            byte[] pdfBytes = Document.Create(container => {
                container.Page(page => {
                    page.Size(PageSizes.A5);
                    page.Margin(15);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col => {
                        col.Item().AlignCenter().Width(80).Image("wwwroot/images/happypawslogo.jpg").FitWidth();
                        col.Item().AlignCenter().Text("Happy Paws Veterinary Clinic").SemiBold().FontSize(14);
                        col.Item().AlignCenter().Text("Pet Health Card").FontSize(10);
                        col.Item().PaddingBottom(5).LineHorizontal(1);
                    });

                    page.Content().Column(col => {
                        col.Item().Row(row => {
                            row.RelativeColumn().Column(info => {
                                info.Item().Text($"Name: {pet.Name}");
                                info.Item().Text($"Species: {pet.Type}");
                                info.Item().Text($"Breed: {pet.Breed}");
                                info.Item().Text($"Birthdate: {pet.Birthdate:MMM dd, yyyy}");
                                info.Item().Text($"Pet ID: {pet.PetID:D6}");
                                info.Item().Text("");
                                info.Item().Text("Owner Information").SemiBold();
                                info.Item().Text($"Name: {pet.Owner.Name}");
                                info.Item().Text($"Contact: {pet.Owner.Phone}");
                                info.Item().Text($"Email: {pet.Owner.Email}");
                            });
                            row.ConstantColumn(90).Border(1).Padding(2).AlignCenter().Image(photoPath).FitArea();
                        });

                        col.Item().PaddingTop(6).LineHorizontal(1);

                        col.Item().PaddingTop(6).Table(table => {
                            table.ColumnsDefinition(cols => {
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(0.8f);
                                cols.RelativeColumn(1.2f);
                                cols.RelativeColumn(1.2f);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                            });

                            table.Header(h => {
                                h.Cell().Text("Date").SemiBold();
                                h.Cell().Text("Wt.").SemiBold();
                                h.Cell().Text("Vaccination").SemiBold();
                                h.Cell().Text("Ecto & Endo").SemiBold();
                                h.Cell().Text("Veterinarian").SemiBold();
                                h.Cell().Text("Next").SemiBold();
                            });

                            foreach (var r in records) {
                                table.Cell().Text(r.AppointmentDate.ToString("MM/dd/yyyy"));
                                table.Cell().Text(r.Notes?.Split("|")?[0] ?? "");
                                table.Cell().Text(
                                    r.ServiceCategory?.ServiceType == "Vaccination"
                                        ? (string.IsNullOrEmpty(r.ServiceSubtype?.ServiceSubType) ? "Not Availed" : r.ServiceSubtype?.ServiceSubType)
                                        : "");
                                table.Cell().Text(
                                    r.ServiceCategory?.ServiceType == "Deworming & Preventives"
                                        ? (string.IsNullOrEmpty(r.ServiceSubtype?.ServiceSubType) ? "Not Availed" : r.ServiceSubtype?.ServiceSubType)
                                        : "");
                                table.Cell().Text(r.AdministeredBy ?? "");
                                table.Cell().Text(r.DueDate?.ToString("MM/dd/yyyy") ?? "");
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text($"Generated {DateTime.Now:MM/dd/yyyy hh:mm tt}");
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"PetCard_{pet.Name}_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}
