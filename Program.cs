using Mecano.Components;
using Mecano.Data;
using Mecano.Logica.Interfaces;
using Mecano.Logica.Servicios;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"CS => [{connectionString}]");
Console.WriteLine($"Archivos cargados: {string.Join(", ", builder.Configuration.Sources.Select(s => s.ToString()))}");

var serverVersion = new MySqlServerVersion(new Version(8, 0, 41));

builder.Services.AddDbContextFactory<MySQLDBContext>(
    DbContextOptions => DbContextOptions
    .UseMySql(connectionString, serverVersion)
    .LogTo(Console.WriteLine, LogLevel.Information)
    .EnableSensitiveDataLogging()
    .EnableDetailedErrors()
    );

// Register application services
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IVehiculoService, VehiculoService>();
builder.Services.AddScoped<IServicioService, ServicioService>();
builder.Services.AddScoped<IMecanicoService, MecanicoService>();
builder.Services.AddScoped<ICitaService, CitaService>();
builder.Services.AddScoped<ICalendarQueryService, CalendarQueryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MySQLDBContext>>();
    var seeder = new DbSeeder(factory);
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
