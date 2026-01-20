using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using PurrVet.Models;

namespace PurrVet.Controllers
{
    public class CalendarController : Controller
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ApplicationDbContext _context;

        public CalendarController(GraphServiceClient graphServiceClient, ApplicationDbContext context)
        {
            _graphServiceClient = graphServiceClient;
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet("Account/ConnectMicrosoft")]
        public IActionResult ConnectMicrosoft()
        {
            var callbackUrl = Url.Action("ConnectMicrosoftCallback", "Calendar", null, Request.Scheme);
            return Challenge(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                OpenIdConnectDefaults.AuthenticationScheme
            );
        }


        [HttpGet("Account/LogoutMicrosoft")]
        public async Task<IActionResult> LogoutMicrosoft()
        {
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var userId = HttpContext.Session.GetInt32("UserID");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId != null)
            {
                var connection = _context.MicrosoftAccountConnections.FirstOrDefault(x => x.UserID == userId);
                if (connection != null)
                {
                    _context.MicrosoftAccountConnections.Remove(connection);
                    _context.SaveChanges();
                    TempData["MicrosoftDisconnected"] = true;
                }
                else
                {
                    TempData["MicrosoftNotConnected"] = true;
                }
            }
            else
            {
                TempData["MicrosoftNotConnected"] = true;
            }

            HttpContext.Session.Remove("MicrosoftEmail");
            return userRole switch
            {
                "Owner" => RedirectToAction("Profile", "Owner"),
                "Staff" => RedirectToAction("Profile", "Staff"),
                _ => RedirectToAction("Profile", "Admin")
            };
        }

        [HttpPost("Admin/SyncAllAppointments")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncAllAppointments()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Json(new { success = false, message = "Syncing failed. Please connect your Microsoft account first." });
            }

            try
            {
                var me = await _graphServiceClient.Me.GetAsync();
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Microsoft Graph authentication failed. Please reconnect your account." });
            }

            var appointments = _context.Appointments
                .Include(a => a.Pet)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .Where(a => !a.IsSynced) 
                .ToList();

            if (!appointments.Any())
            {
                return Json(new { success = true, message = "All appointments are already synced." });
            }

            int successCount = 0, failureCount = 0;

            foreach (var appt in appointments)
            {
                try
                {
                    string subject = $"Vet Appointment - {appt.ServiceCategory?.ServiceType}";
                    if (appt.ServiceSubtype != null)
                        subject += $" ({appt.ServiceSubtype.ServiceSubType})";

                    var @event = new Event
                    {
                        Subject = subject,
                        Body = new ItemBody
                        {
                            ContentType = BodyType.Html,
                            Content = appt.Notes ?? "Vet appointment details"
                        },
                        Start = new DateTimeTimeZone
                        {
                            DateTime = appt.AppointmentDate.ToUniversalTime().ToString("o"),
                            TimeZone = "UTC"
                        },
                        End = new DateTimeTimeZone
                        {
                            DateTime = appt.AppointmentDate.AddMinutes(30).ToUniversalTime().ToString("o"),
                            TimeZone = "UTC"
                        },
                        Location = new Location { DisplayName = "PurrVet Veterinary Clinic" }
                    };

                    await _graphServiceClient.Me.Events.PostAsync(@event);

                    appt.IsSynced = true;
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to sync appointment {appt.AppointmentID}: {ex.Message}");
                    failureCount++;
                }
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"{successCount} new appointments synced. {failureCount} failed.",
                synced = successCount,
                failed = failureCount
            });
        }

        [HttpPost("Owner/SyncAppointments")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncOwnerAppointments()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Json(new
                {
                    success = false,
                    message = "You are not connected to a Microsoft account. Please connect your calendar to sync appointments."
                });
            }

            var ownerId = HttpContext.Session.GetInt32("OwnerID");
            if (ownerId == null)
            {
                return Json(new { success = false, message = "Owner not found in session." });
            }

            var appointments = _context.Appointments
                .Include(a => a.Pet)
                .ThenInclude(p => p.Owner)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .Where(a => a.Pet.OwnerID == ownerId && !a.IsSynced)
                .ToList();

            if (!appointments.Any())
            {
                return Json(new { success = true, message = "All appointments are already synced." });
            }

            int successCount = 0, failureCount = 0;

            try
            {
                foreach (var appointment in appointments)
                {
                    if (appointment.Pet == null)
                    {
                        failureCount++;
                        continue;
                    }

                    string subject = $"Vet Appointment - {appointment.ServiceCategory?.ServiceType}";
                    if (appointment.ServiceSubtype != null)
                        subject += $" ({appointment.ServiceSubtype.ServiceSubType})";

                    var @event = new Event
                    {
                        Subject = subject,
                        Body = new ItemBody
                        {
                            ContentType = BodyType.Html,
                            Content = appointment.Notes ?? "Vet appointment details"
                        },
                        Start = new DateTimeTimeZone
                        {
                            DateTime = appointment.AppointmentDate.ToUniversalTime().ToString("o"),
                            TimeZone = "UTC"
                        },
                        End = new DateTimeTimeZone
                        {
                            DateTime = appointment.AppointmentDate.AddMinutes(30).ToUniversalTime().ToString("o"),
                            TimeZone = "UTC"
                        },
                        Location = new Location
                        {
                            DisplayName = "PurrVet Veterinary Clinic"
                        }
                    };

                    try
                    {
                        await _graphServiceClient.Me.Events.PostAsync(@event);
                        appointment.IsSynced = true;
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error syncing owner appointment {appointment.AppointmentID}: {ex.Message}");
                        failureCount++;
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"{successCount} new appointments synced. {failureCount} failed.",
                    synced = successCount,
                    failed = failureCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error syncing appointments: {ex.Message}" });
            }
        }

        [HttpGet("Admin/TestGraph")]
        public async Task<IActionResult> TestGraph()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Json(new { success = false, message = "Not connected to Microsoft account." });
            }

            try
            {
                var me = await _graphServiceClient.Me.GetAsync();
                return Json(new { success = true, name = me?.DisplayName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Admin/TestSingleSync")]
        public async Task<IActionResult> TestSingleSync()
        {
            var appointment = _context.Appointments
                .Include(a => a.Pet)
                .Include(a => a.ServiceCategory)
                .Include(a => a.ServiceSubtype)
                .FirstOrDefault();

            if (appointment == null)
                return Json(new { success = false, message = "No appointments found." });

            if (appointment.Pet == null)
                return Json(new { success = false, message = "Appointment has no linked pet." });

            string subject = $"Test Sync - {appointment.ServiceCategory?.ServiceType}";
            if (appointment.ServiceSubtype != null)
                subject += $" ({appointment.ServiceSubtype.ServiceSubType})";

            var @event = new Event
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = appointment.Notes ?? "Vet appointment details"
                },
                Start = new DateTimeTimeZone
                {
                    DateTime = appointment.AppointmentDate.ToUniversalTime().ToString("o"),
                    TimeZone = "UTC"
                },
                End = new DateTimeTimeZone
                {
                    DateTime = appointment.AppointmentDate.AddMinutes(30).ToUniversalTime().ToString("o"),
                    TimeZone = "UTC"
                },
                Location = new Location
                {
                    DisplayName = "Happy Paws Veterinary Clinic"
                }
            };

            try
            {
                await _graphServiceClient.Me.Events.PostAsync(@event);
                return Json(new { success = true, message = "Single appointment synced!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("SINGLE SYNC FAILED:");
                Console.WriteLine(ex.ToString());
                return Json(new { success = false, message = $"Error: {ex.Message}", details = ex.InnerException?.Message, stack = ex.StackTrace });
            }
        }
        [HttpGet]
        public async Task<IActionResult> ConnectMicrosoftCallback()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var me = await _graphServiceClient.Me.GetAsync();

            var authResult = await HttpContext.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme);
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var refreshToken = await HttpContext.GetTokenAsync("refresh_token");
            var expiresAtStr = await HttpContext.GetTokenAsync("expires_at");
            DateTime? expiry = null;

            if (DateTime.TryParse(expiresAtStr, out var parsed))
                expiry = parsed;

            var existing = await _context.MicrosoftAccountConnections
                .FirstOrDefaultAsync(x => x.UserID == userId);

            if (existing == null)
            {
                _context.MicrosoftAccountConnections.Add(new MicrosoftAccountConnection
                {
                    UserID = userId.Value,
                    MicrosoftEmail = me?.Mail ?? me?.UserPrincipalName ?? "unknown",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    TokenExpiry = expiry,
                    ConnectedAt = DateTime.Now,
                    IsAutoSyncEnabled = false
                });
            }
            else
            {
                existing.MicrosoftEmail = me?.Mail ?? me?.UserPrincipalName ?? "unknown";
                existing.AccessToken = accessToken;
                existing.RefreshToken = refreshToken;
                existing.TokenExpiry = expiry;
                existing.ConnectedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["MicrosoftConnected"] = true;

            if (userRole == "Owner")
                return RedirectToAction("Profile", "Owner");
            else if (userRole == "Staff")
                return RedirectToAction("Profile", "Staff");
            else
                return RedirectToAction("Profile", "Admin");
        }

    }
}
