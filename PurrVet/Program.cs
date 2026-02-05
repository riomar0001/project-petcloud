using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Kiota.Http.HttpClientLibrary;
using PurrVet.DTOs.Common;
using PurrVet.Infrastructure;
using PurrVet.Models;
using PurrVet.Services;
using System.Globalization;
using System.Text;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options => {
        builder.Configuration.Bind("AzureAd", options);
        options.SaveTokens = true;
        options.UseTokenLifetime = false;
        options.Events ??= new OpenIdConnectEvents();
        options.Events.OnTokenValidated = context => {
            context.Properties.IsPersistent = true;
            return Task.CompletedTask;
        };
    })
    .EnableTokenAcquisitionToCallDownstreamApi(new[] { "User.Read", "Calendars.ReadWrite" })
    .AddMicrosoftGraph(builder.Configuration.GetSection("Graph"))
    .AddInMemoryTokenCaches();

// JWT Bearer auth for mobile API
builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options => {
    options.AddPolicy("OwnerOnly", policy => {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireRole("Owner");
        policy.RequireClaim("ownerId");
    });
});

builder.Services.AddHttpClient<SmsReminderService>();
builder.Services.Configure<GmailSettings>(builder.Configuration.GetSection("Gmail"));
builder.Services.AddTransient<EmailService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddScoped<GraphServiceClient>(sp => {
    var tokenAcquisition = sp.GetRequiredService<ITokenAcquisition>();
    var authProvider = new TokenAcquisitionAuthenticationProvider(tokenAcquisition);
    var adapter = new HttpClientRequestAdapter(authProvider) {
        BaseUrl = builder.Configuration["Graph:BaseUrl"] ?? "https://graph.microsoft.com/v1.0"
    };
    return new GraphServiceClient(adapter);
});

builder.Services.AddCors(options => {
    options.AddPolicy("MobileApp", policy => {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllersWithViews();
builder.Services.Configure<ApiBehaviorOptions>(options => {
    options.InvalidModelStateResponseFactory = context => {
        var errors = context.ModelState
            .Where(e => e.Value != null && e.Value.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray()
            );
        return new BadRequestObjectResult(new ApiErrorResponse {
            Success = false,
            Message = "Validation failed.",
            Errors = errors
        });
    };
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddDistributedMemoryCache();
builder.Services.ConfigureApplicationCookie(options => {
    options.Cookie.Name = "PurrVetAuth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
});

builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.Name = "PurrVetSession";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.MaxAge = TimeSpan.FromDays(30);
});


var app = builder.Build();

if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error/Error");
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
}

app.UseMiddleware<ApiExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("MobileApp");
app.UseSession();

// Session restoration middleware — skip for API routes
app.Use(async (context, next) => {
    if (context.Request.Path.StartsWithSegments("/api")) {
        await next();
        return;
    }

    if (context.User.Identity?.IsAuthenticated == true &&
        !context.Session.Keys.Contains("UserID")) {
        var userId = context.User.FindFirst("UserID")?.Value;
        var userName = context.User.FindFirst("UserName")?.Value;
        var userRole = context.User.FindFirst("UserRole")?.Value;
        var profileImage = context.User.FindFirst("ProfileImage")?.Value;

        if (userId != null) {
            context.Session.SetInt32("UserID", int.Parse(userId));
            context.Session.SetString("UserName", userName ?? "");
            context.Session.SetString("UserRole", userRole ?? "");
            context.Session.SetString("ProfileImage", profileImage ?? "golden.png");
        }
    }

    await next();
});

// Redirect middleware — skip for API routes
app.Use(async (context, next) => {
    if (context.Request.Path.StartsWithSegments("/api")) {
        await next();
        return;
    }

    var path = context.Request.Path.Value?.ToLower();

    if (path == "/account/home" || path == "/account/login" || path == "/account/register") {
        var userRole = context.Session.GetString("UserRole");
        if (!string.IsNullOrEmpty(userRole)) {
            var redirectUrl = userRole switch {
                "Admin" => "/Admin/Dashboard",
                "Owner" => "/Owner/Dashboard",
                "Staff" => "/Staff/Dashboard",
                _ => "/Account/Home"
            };

            context.Response.Redirect(redirectUrl);
            return;
        }
    }

    await next();
});

app.Use(async (context, next) => {
    if (context.Request.Path.StartsWithSegments("/api")) {
        await next();
        return;
    }

    if (context.Request.Path == "/") {
        var userRole = context.Session.GetString("UserRole");
        if (!string.IsNullOrEmpty(userRole)) {
            var redirectUrl = userRole switch {
                "Admin" => "/Admin/Dashboard",
                "Owner" => "/Owner/Dashboard",
                "Staff" => "/Staff/Dashboard",
                _ => "/Account/Home"
            };

            context.Response.Redirect(redirectUrl);
            return;
        } else {
            context.Response.Redirect("/Account/Home");
            return;
        }
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Home}/{id?}")
    .WithStaticAssets();
app.MapFallbackToController("HandleError", "Error");

using (var scope = app.Services.CreateScope()) {
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "MonthlySummary_HappyPaws.csv");

    // 1. read excel
    List<ServiceDemandData> csvHistory;
    using (var reader = new StreamReader(csvPath))
    using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture))) {
        csv.Context.RegisterClassMap<ServiceDemandDataMap>();
        csvHistory = csv.GetRecords<ServiceDemandData>().ToList();
    }

    // 2. merge db data witg csv
    var dbSummary = context.Appointments
        .Where(a => a.Status == "Completed" && a.CategoryID != null)
        .GroupBy(a => new { a.AppointmentDate.Year, a.AppointmentDate.Month, Category = a.ServiceCategory.ServiceType })
        .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Category, Count = g.Count() })
        .ToList();

    foreach (var group in dbSummary) {
        var existing = csvHistory.FirstOrDefault(x => x.Year == group.Year && x.Month == group.Month);
        if (existing == null) {
            existing = new ServiceDemandData { Year = group.Year, Month = group.Month };
            csvHistory.Add(existing);
        }

        switch (group.Category) {
            case "Confinement / Hospitalization": existing.Confinement += group.Count; break;
            case "Deworming & Preventives": existing.Deworming += group.Count; break;
            case "End of Life Care": existing.EndOfLifeCare += group.Count; break;
            case "Grooming & Wellness": existing.Grooming += group.Count; break;
            case "Medication & Treatment": existing.Medication += group.Count; break;
            case "Professional Fee / Consultation": existing.Consultation += group.Count; break;
            case "Specialty Tests / Rare Cases": existing.SpecialtyTests += group.Count; break;
            case "Surgery": existing.Surgery += group.Count; break;
            case "Vaccination": existing.Vaccination += group.Count; break;
            case "Diagnostics & Laboratory Tests": existing.Diagnostics += group.Count; break;
        }
    }

    // sorted merged data
    csvHistory = csvHistory.OrderBy(x => x.Year).ThenBy(x => x.Month).ToList();

    // 3. train then predict
    var forecastService = new ForecastService(csvPath);
    var result = forecastService.TrainAndPredict(csvHistory, "All", "All");

    double averageActual = result.ActualCounts.Average();
    double mae = result.ActualCounts.Zip(result.PredictedCounts, (actual, pred) => Math.Abs(actual - pred)).Average();
    double accuracyPercentage = 100 * (1 - (mae / averageActual));

    Console.WriteLine($"Approximate Accuracy: {accuracyPercentage:0.##}%");
    Console.WriteLine("Eval Metrics:");
    Console.WriteLine($"Next Forecast Month: {result.NextMonth}");
    Console.WriteLine($"Next Forecast Value: {result.NextForecastValue}");
    Console.WriteLine($"Severity: {result.Severity}");

    //var exportFolder = Path.Combine(Directory.GetCurrentDirectory(), "ExportedModels");
    // var exporter = new ModelExporter(csvPath, exportFolder);
    //exporter.ExportAllServiceModels();
    // Console.WriteLine("All models exported!");
}
app.Run();
