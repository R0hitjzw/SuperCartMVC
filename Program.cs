using SuperCartMVC.Services.Finders;
using SuperCartMVC.Services.Finders.Impl;

// LOG IN DEPENDENCIES, AFEGIR A LA DI (injecció de dependències) PERQUE DESPRES EL CONTROLADOR PUGUI UTILITZAR-LOS
using SuperCartMVC.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Los mismos Finders que tenías
builder.Services.AddScoped<IFinder, MercadonaFinder>();
builder.Services.AddScoped<IFinder, AldiFinder>();
builder.Services.AddScoped<IFinder, ConsumFinder>();
builder.Services.AddScoped<IFinder, AmetllerFinder>();
builder.Services.AddScoped<IFinder, AlcampoFinder>();
builder.Services.AddScoped<IFinder, BonpreuFinder>();
builder.Services.AddScoped<IFinder, DiaFinder>();
builder.Services.AddScoped<IFinder, CondisFinder>();


// SISTEMA DE ROTACIÓ DE API-KEYS D'ANTHROPIC - que amb el rate limit, la app funciona irregularment.
builder.Services.AddSingleton<SuperCartMVC.Services.AnthropicKeyRotator>();

// TRADUCTOR ES→CA PER BONPREU - singleton amb caché estàtica, reutilitza entre requests
builder.Services.AddSingleton<SuperCartMVC.Services.CatalanTranslatorService>();


// REGISTRO DE SERVICIOS PARA LOG IN, AFEGIR A LA DI (injecció de dependències) PERQUE DESPRES EL CONTROLADOR PUGUI UTILITZAR-LOS
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
                  ?? Environment.GetEnvironmentVariable("DATABASE_URL")!;
    options.UseNpgsql(connStr);
});

// configuració requirements de contrasenya, un digit minim, logitud, etc.
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>()
    .SetApplicationName("SuperCartMVC");

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();