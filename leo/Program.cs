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
    //option.IdleTimeout = TimeSpan.FromMinutes(59);
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
