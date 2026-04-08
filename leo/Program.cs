using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using leo.Data;
using leo.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using leo.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<leoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("leoContext") ?? throw new InvalidOperationException("Connection string 'leoContext' not found.")));


//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddControllersWithViews();
//builder.Services.AddSwaggerGen(options =>
//{
//    options.SwaggerDoc("v1",
//        new Microsoft.OpenApi.Models.OpenApiInfo
//        {
//            Title = "New Swagger",
//            Description = " New Swagger Document",
//            Version = "1"
//        });
// Register your services here


builder.Services.AddLogging(); // Ensure logging services are added

builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(option =>
{
    option.Cookie.Name = "LEOTECH101.Session";
    option.IdleTimeout = TimeSpan.FromMinutes(59);
    option.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.AccessDeniedPath = "/Account/AccessDenied"; // Ensure this path is correct
    });
builder.Services.AddScoped<AuditLogService>(); // Register your AuditLogService

builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllersWithViews();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1",
        new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "New Swagger",
            Description = " New Swagger Document",
            Version = "1"
        });
});



// Register the ReturnService
builder.Services.AddScoped<ReturnService>();
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddScoped<AuditLogService>();

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<leoContext>();
    dbContext.Database.Migrate();

    // --- SEED DATA ---
    // 1. Ensure Roles exist
    if (!dbContext.Role.Any(r => r.RoleName == "Admin"))
    {
        dbContext.Role.Add(new Role { RoleName = "Admin" });
        if (!dbContext.Role.Any(r => r.RoleName == "Staff"))
        {
            dbContext.Role.Add(new Role { RoleName = "Staff" });
        }
        dbContext.SaveChanges();
    }

    // 2. Ensure Default Admin User exists
    var adminRole = dbContext.Role.FirstOrDefault(r => r.RoleName == "Admin");
    if (adminRole != null && !dbContext.Users.Any(u => u.Username == "admin"))
    {
        var adminUser = new Users
        {
            FirstName = "System",
            LastName = "Admin",
            Email = "admin123@gmail.com",
            Username = "admin",
            Password = HashingServices.HashData("admin"), // Direct requested password "admin"
            RoleId = adminRole.RoleId
        };
        dbContext.Users.Add(adminUser);
        dbContext.SaveChanges();
    }
    // -----------------
}
catch (Exception ex)
{
    // Log error if needed: Console.WriteLine(ex.Message);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseDeveloperExceptionPage();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();
