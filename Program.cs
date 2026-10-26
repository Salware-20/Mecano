using ApexCharts;
using Mecano.Components;
using Mecano.Components.Shared;
using Mecano.Data;
using Mecano.Endpoints;
using Mecano.Entidad.Constantes;
using Mecano.Logica.Interfaces;
using Mecano.Logica.Servicios;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Archivos cargados: {string.Join(", ", builder.Configuration.Sources.Select(s => s.ToString()))}");

var serverVersion = new MySqlServerVersion(new Version(8, 0, 41));

builder.Services.AddDbContextFactory<MySQLDBContext>(
    DbContextOptions => {
        DbContextOptions.UseMySql(connectionString, serverVersion)
            .LogTo(Console.WriteLine, LogLevel.Information);
            
        // Seguridad: Protegemos la exposición de datos sensibles sólo para desarrollo
        if (builder.Environment.IsDevelopment())
        {
            DbContextOptions.EnableSensitiveDataLogging()
                            .EnableDetailedErrors();
        }
    });

// 1. Configuración de Autenticación por Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/acceso-denegado";
    });

// 2. Configuración de Autorización y Políticas
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administrador", policy => policy.RequireRole(Roles.Administrador));
    options.AddPolicy("AdminGlobal", policy => 
        policy.RequireRole(Roles.Administrador).RequireClaim("EsAdminGlobal", "true"));
});

// 3. Configuración de Blazor Auth
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Register application services
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IVehiculoService, VehiculoService>();
builder.Services.AddScoped<IServicioService, ServicioService>();
builder.Services.AddScoped<IMecanicoService, MecanicoService>();
builder.Services.AddScoped<ICitaService, CitaService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddApexCharts(); // Servicio opcional de ApexCharts (localización/globales).
builder.Services.AddScoped<ThemeState>(); // Tema claro/oscuro compartido en el circuito.
builder.Services.AddScoped<ICalendarQueryService, CalendarQueryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IComentarioService, ComentarioService>();
builder.Services.AddScoped<IAuthServices, AuthServices>();
builder.Services.AddScoped<IAdministradorService, AdministradorService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();

builder.Services.AddScoped<DbSeeder>();

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.InitializeAsync();
    await seeder.SeedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
// Páginas de estado para el sitio (404, 403, etc.) re-ejecutadas hacia /not-found.
// Se excluye /api/*: re-ejecutar una respuesta de la API (p. ej. el 401 del login)
// contra un endpoint de componente Blazor la convertiría en un 400 engañoso
// ("The request has an incorrect Content-type."), porque Blazor intenta leer el
// cuerpo del POST como formulario. Los endpoints de API deben responder su propio
// código/estado sin re-ejecución.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    rama => rama.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

// Routing explícito DESPUÉS de las páginas de estado: el reexecute necesita
// volver a resolver el endpoint de /not-found. Si WebApplication inserta
// UseRouting al inicio del pipeline, el reexecute corre con endpoint nulo y el
// 404 sale con cuerpo vacío (el navegador muestra su propia página de error).
app.UseRouting();

app.UseHttpsRedirection();

app.UseAntiforgery();

// Middlewares en el Pipeline
app.UseAuthentication();
app.UseAuthorization();

// Mapeo de Endpoints de Autenticación Minimal API
app.MapAuthEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
