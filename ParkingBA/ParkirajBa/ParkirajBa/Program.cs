using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Services;
using ParkirajBa.Hubs;

namespace ParkirajBa
{
    public class Program
    {
        static async Task SeedRolesAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roles = { "User", "Admin", "Owner" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        static async Task SeedAdminAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string adminEmail = "admin@parkirajba.ba";
            string adminPassword = "Admin123!";

            var existing = await userManager.FindByEmailAsync(adminEmail);
            if (existing == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "ParkirajBa",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Database
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // 2. Identity 
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;    // required confirmed account
                options.SignIn.RequireConfirmedEmail = true;
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/User/Login";
                options.AccessDeniedPath = "/User/AccessDenied";
                options.LogoutPath = "/User/Logout";
            });

            // 3. Email sender (Identity IEmailSender)
            builder.Services.AddTransient<IEmailSender, EmailSender>();
            builder.Services.AddHostedService<ReservationReminderService>();

            // ImageService
            builder.Services.AddTransient<ImageService>();

            // 4. Repository
            builder.Services.AddScoped<ParkirajBa.Repositories.IParkingRepository, ParkirajBa.Repositories.ParkingRepository>();

            builder.Services.AddScoped<ParkirajBa.Repositories.IRequestRepository, ParkirajBa.Repositories.RequestRepository>();

            //Za provjeru ticket-a da li treba rezervisat
            builder.Services.AddHostedService<ParkingReservationBackgroundService>();

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            //Checks status of Ticket
            builder.Services.AddHostedService<OverstayChargeService>();
            builder.Services.AddSignalR(); // za slanje signala pri promjeni baze podataka
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            //Za testiranje ulaza na parking preko mobitela
            //builder.WebHost.UseUrls("http://0.0.0.0:5100");
            //------------------------

            var app = builder.Build();

            await SeedRolesAsync(app);
            await SeedAdminAsync(app);

            if (app.Environment.IsDevelopment())
                app.UseMigrationsEndPoint();
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-US"),
                SupportedCultures = new[] { System.Globalization.CultureInfo.InvariantCulture },
                SupportedUICultures = new[] { System.Globalization.CultureInfo.InvariantCulture }
            });
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.MapControllers(); //Damir dodao, provjeriti da li treba
            app.MapHub<ParkingHub>("/parkingHub");
            app.Run();
        }
    }
}
