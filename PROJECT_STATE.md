# PROJECT STATE: Mecano

## 1. Resumen ejecutivo

| Campo | Valor |
|---|---|
| **Nombre del proyecto** | Mecano |
| **Propósito** | Aplicación de taller mecánico para agendar citas, gestionar clientes, vehículos, servicios y mecánicos. |
| **Stack principal** | .NET 10.0 (SDK Web), Blazor Server (Interactive Server), Entity Framework Core 9.0, MySQL 8.0 (Pomelo EF Provider) |
| **Estado general** | Prototipo / MVP — funcionalidades básicas de gestión de citas operativas, con autenticación por cookies y autorización por roles implementadas. |
| **Páginas Blazor** | 10 páginas (.razor) entre Components/Pages y Components/Layout |
| **Entidades de dominio** | 8 clases principales: Administrador, Mecanico, Cliente, Vehiculo, Servicio, Categoria, Cita, NotificacionLog |

---

## 2. Estructura de la solución

El repositorio contiene un único proyecto de .NET:

```
Mecano/
├─ Mecano.csproj              # Proyecto principal, TargetFramework net10.0, Web SDK
├─ Mecano.slnx                # Solution file (un solo proyecto)
├─ Program.cs                 # Punto de entrada, configuración de servicios y pipeline HTTP
├─ appsettings.json           # Configuración de cadenas de conexión y logging
├─ appsettings.Development.json
├─ Properties/
│  └─ launchSettings.json    # Perfiles de arranque (http/https, puertos 5023/7274)
├─ Entidad/
│  ├─ Clases/                # Entidades EF Core (POCOs)
│  │  ├─ Administrador.cs
│  │  ├─ Mecanico.cs
│  │  ├─ Cliente.cs
│  │  ├─ Vehiculo.cs
│  │  ├─ Cita.cs
│  │  ├─ Servicio.cs
│  │  └─ Categoria.cs
│  ├─ DTOs/                  # Objetos de transferencia de datos
│  │  ├─ AuthenticatedUser.cs
│  │  ├─ LoginDTO.cs
│  │  ├─ RegistroAdminDTO.cs
│  │  ├─ AgendarCitaDTO.cs
│  │  ├─ HorarioDisponibleDTO.cs
│  │  ├─ CalendarEventDTO.cs
│  │  └─ NuevoVehiculoDTO.cs
│  ├─ Excepciones/
│  │  ├─ InactiveMechanicException.cs
│  │  └─ AppointmentOverlapException.cs
│  └─ Constantes/
│     └─ Roles.cs            # Strings centralizados: "Administrador", "Mecanico"
├─ Data/
│  ├─ MySQLDBContext.cs     # DbContext con DbSet<>
│  ├─ DbSeeder.cs           # Seed de datos iniciales (categorías, servicios, mecánicos, etc.)
│  └─ Migrations/           # Migración inicial EF Core (20261003033206_Inicial)
├─ Endpoints/
│  └─ AuthEndpoints.cs      # Minimal API: POST /api/auth/login, POST /api/auth/logout
├─ Logica/
│  ├─ Interfaces/           # Firmas de servicio (I*Service, IAuthServices)
│  └─ Servicios/            # Implementaciones (Scoped en DI)
│     ├─ AuthServices.cs
│     ├─ CustomAuthenticationStateProvider.cs  # Revalida sesión cada 10 min
│     ├─ ClienteService.cs
│     ├─ MecanicoService.cs
│     ├─ CitaService.cs
│     ├─ ServicioService.cs
│     ├─ CategoriaService.cs
│     ├─ CalendarQueryService.cs
│     ├─ NotificationService.cs
│     └─ VehiculoService.cs
├─ wwwroot/js/
│  ├─ auth.js               # window.authFetch.login/logout (fetch con credentials same-origin)
│  └─ fullcalendar-interop.js
├─ Components/
│  ├─ _Imports.razor          # Directivas using compartidas
│  ├─ Routes.razor           # Configuración del enrutador
│  ├─ App.razor              # Raíz HTML con script de framework Blazor
│  ├─ Layout/
│  │  ├─ MainLayout.razor    # Sidebar + NavMenu + @Body
│  │  ├─ NavMenu.razor      # Enlaces: Home, TEST, Agendar
│  │  └─ ReconnectModal.razor
│  └─ Pages/
│     ├─ Home.razor          # @page "/", [Authorize]
│     ├─ Counter.razor       # @page "/counter", [Authorize], InteractiveServer
│     ├─ Weather.razor       # @page "/weather", [Authorize], [StreamRendering]
│     ├─ AgendarCita.razor  # @page "/agendar", [Authorize(Roles=Administrador)], InteractiveServer
│     ├─ Login.razor         # @page "/login", [AllowAnonymous], InteractiveServer
│     ├─ AccesoDenegado.razor # @page "/acceso-denegado", [AllowAnonymous]
│     ├─ NotFound.razor      # @page "/not-found"
│     ├─ Error.razor         # @page "/Error"
│     └─ ConnectionTest.razor # @page "/connectiontest", [Authorize(Roles=Administrador)], InteractiveServer
│  └─ Shared/
│     └─ RedirectToLogin.razor # Redirige a /login cuando no hay auth state
```

**Responsabilidad de cada proyecto (solo uno):**
- **Mecano.csproj**: Contiene todo — UI (Blazor Server), lógica de negocio (Services), entidades, acceso a datos (DbContext), y configuración. Es una arquitectura "full-stack" en un solo proyecto.

**Referencias entre componentes:**
- Program.cs configura todos los servicios DI y el pipeline HTTP.
- MySQLDBContext es inyectado vía `IDbContextFactory<MySQLDBContext>` en los servicios.
- AuthServices usa el factory para crear contextos bajo demanda.
- Los componentes Razor inyectan interfaces de servicio (ej. `@inject IClienteService ClienteService`).
- NavMenu enlaza a rutas definidas con `@page` en los componentes.

---

## 3. Stack técnico y dependencias

### .NET version
- **TargetFramework**: `net10.0` (desde `Mecano.csproj:4`)
- **Nullable**: `enable` (csproj:5)
- **ImplicitUsings**: `enable` (csproj:6)
- **BlazorDisableThrowNavigationException**: true (csproj:7)
- **SelfContained**: true, **RuntimeIdentifier**: `win-x64` (deployment self-contained)
- **UserSecretsId**: `ee6ffb4c-1aed-40a4-88b5-09d68d4b6165`
- **Herramienta local**: `dotnet-ef` 10.0.12 (declarada en `dotnet-tools.json`)

### Paquetes NuGet

| Proyecto | Paquete | Versión |
|---|---|---|
| Mecano | Microsoft.EntityFrameworkCore.Design | 9.0.20 |
| Mecano | Microsoft.EntityFrameworkCore.Relational | 9.0.20 |
| Mecano | Pomelo.EntityFrameworkCore.MySql | 9.0.0 |

### Servicios externos
- **Base de datos**: MySQL 8.0.41 ( configurada en `appsettings.json` como `server=localhost;user=root;password=;database=mecano`)
- No hay servicios de email configurados (NotificationService usa logging simulado).
- No hay servicios de almacenamiento en la nube.

---

## 4. Configuración y arranque

### Program.cs (contenido completo)

```csharp
using Mecano.Components;
using Mecano.Data;
using Mecano.Endpoints;
using Mecano.Entidad.Constantes;
using Mecano.Logica.Interfaces;
using Mecano.Logica.Servicios;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var serverVersion = new MySqlServerVersion(new Version(8, 0, 41));

builder.Services.AddDbContextFactory<MySQLDBContext>(DbContextOptions =>
{
    DbContextOptions.UseMySql(connectionString, serverVersion)
        .LogTo(Console.WriteLine, LogLevel.Information);
    if (builder.Environment.IsDevelopment())
    {
        DbContextOptions.EnableSensitiveDataLogging().EnableDetailedErrors();
    }
});

// Autenticación por cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/acceso-denegado";
    });

// Políticas de autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administrador", p => p.RequireRole(Roles.Administrador));
    options.AddPolicy("AdminGlobal", p => p.RequireRole(Roles.Administrador)
        .RequireClaim("EsAdminGlobal", "true"));
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Servicios de aplicación
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IVehiculoService, VehiculoService>();
builder.Services.AddScoped<IServicioService, ServicioService>();
builder.Services.AddScoped<IMecanicoService, MecanicoService>();
builder.Services.AddScoped<ICitaService, CitaService>();
builder.Services.AddScoped<ICalendarQueryService, CalendarQueryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IAuthServices, AuthServices>();
builder.Services.AddScoped<DbSeeder>();

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": { "DefaultConnection": "server=localhost;user=root;password=;database=mecano" }
}
```

### appsettings.Development.json

Igual que `appsettings.json` — `DefaultConnection` con `password=` vacío.

### User-secrets
- **Configurado**: `UserSecretsId` presente en `Mecano.csproj`. El seeder lee `Seed:GlobalAdmin:Email` y `Seed:GlobalAdmin:Password` desde la configuración (user-secrets o variables de entorno), con defaults `adminglobal@mecano.cr` / `AdminGlobal123*`.

### Variables de entorno
- `ASPNETCORE_ENVIRONMENT` = `Development` (definido en `launchSettings.json:10,19` para perfiles http y https).
- No hay variables adicionales documentadas en el repo.

### Cómo se arranca la app
- **Perfiles**: `http` (puerta 5023) y `https` (puerta 7274 + 5023).
- **Comando**: `dotnet run` sobre el proyecto `Mecano.csproj`.
- **URLs**: `http://localhost:5023` o `https://localhost:7274`.
- **Navegador**: Se lanza automáticamente con ambos perfiles.

---

## 5. Capa de datos

### MySQLDBContext (contenido completo)

```csharp
using Mecano.Entidad.Clases;
using Microsoft.EntityFrameworkCore;

namespace Mecano.Data
{
    public class MySQLDBContext : DbContext
    {
        public MySQLDBContext(DbContextOptions<MySQLDBContext> options) : base(options) { }

        public DbSet<Administrador> Administradors { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Cita> Cita { get; set; }
        public DbSet<Cliente> Cliente { get; set; }
        public DbSet<Mecanico> Mecanico { get; set; }
        public DbSet<NotificacionLog> NotificacionLog { get; set; }
        public DbSet<Servicio> Servicio { get; set; }
        public DbSet<Vehiculo> Vehiculo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cliente - Cita: One client has many appointments
            modelBuilder.Entity<Cliente>()
                .HasMany(c => c.Citas)
                .WithOne(ci => ci.Cliente)
                .HasForeignKey(ci => ci.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cliente - Vehiculo: One client has many vehicles
            modelBuilder.Entity<Cliente>()
                .HasMany(c => c.Vehiculos)
                .WithOne(v => v.Cliente)
                .HasForeignKey(v => v.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Vehiculo - Citas
            modelBuilder.Entity<Vehiculo>()
                .HasMany(v => v.Citas)
                .WithOne(c => c.Vehiculo)
                .HasForeignKey(c => c.VehiculoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mecanico - Cita
            modelBuilder.Entity<Mecanico>()
                .HasMany(m => m.Citas)
                .WithOne(ci => ci.Mecanico)
                .HasForeignKey(ci => ci.MecanicoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mecanico - Especialidad (Categoria)
            modelBuilder.Entity<Mecanico>()
                .HasOne(m => m.Especialidad)
                .WithMany(c => c.Mecanicos)
                .HasForeignKey(m => m.EspecialidadId)
                .OnDelete(DeleteBehavior.Restrict);

            // Servicio - Cita
            modelBuilder.Entity<Servicio>()
                .HasMany(s => s.Citas)
                .WithOne(ci => ci.Servicio)
                .HasForeignKey(ci => ci.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Categoria - Servicio
            modelBuilder.Entity<Categoria>()
                .HasMany(c => c.servicios)
                .WithOne(s => s.Categoria)
                .HasForeignKey(s => s.CategoriaId)
                .OnDelete(DeleteBehavior.SetNull);

            // Cita - NotificacionLog
            modelBuilder.Entity<Cita>()
                .HasMany(c => c.Notificaciones)
                .WithOne(nl => nl.Cita)
                .HasForeignKey(nl => nl.CitaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique index on Cliente.CedulaIdentidad
            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.CedulaIdentidad)
                .IsUnique();

            // Unique index on Vehiculo.Placa
            modelBuilder.Entity<Vehiculo>()
                .HasIndex(v => v.Placa)
                .IsUnique();
        }
    }
}
```

### Entidades (`DbSet<T>` en el DbContext)

| Entidad | DbSet | Comentario |
|---|---|---|
| Administrador | `DbSet<Administrador>` | Usuario con credenciales (email/hash password) |
| Categoria | `DbSet<Categoria>` | Agrupación de servicios |
| Cita | `DbSet<Cita>` | Consulta de horarios, disponibilidad |
| Cliente | `DbSet<Cliente>` | No tiene login/credenciales |
| Mecanico | `DbSet<Mecanico>` | Usuario con credenciales (email/hash password) |
| NotificacionLog | `DbSet<NotificacionLog>` | Log de notificaciones enviadas |
| Servicio | `DbSet<Servicio>` | Servicios ofrecidos por el taller |

### Relaciones configuradas en `OnModelCreating`

- **Cliente → Cita**: 1:N (Cascade delete)
- **Cliente → Vehiculo**: 1:N (Cascade delete)
- **Vehiculo → Cita**: 1:N (Restrict)
- **Mecanico → Cita**: 1:N (Restrict)
- **Mecanico → Categoria (Especialidad)**: 1:N (Restrict)
- **Servicio → Cita**: 1:N (Restrict)
- **Categoria → Servicio**: 1:N (SetNull)
- **Cita → NotificacionLog**: 1:N (Cascade)

### Índices únicos definidos
1. `Cliente.CedulaIdentidad` — Unique index.
2. `Vehiculo.Placa` — Unique index.

### Estado de migraciones
- **Carpeta `Data/Migrations/`**: Existe con la migración inicial `20261003033206_Inicial.cs` (+ `.Designer.cs` y `MySQLDBContextModelSnapshot.cs`).
- **¿Migraciones aplicadas a la BD?**: El seeder usa `Database.EnsureCreatedAsync()` en lugar de `MigrateAsync()`, por lo que el esquema se crea directamente; la migración existe como historial pero no se aplica vía middleware.
- **Estrategia de acceso a BD**: `IDbContextFactory<MySQLDBContext>` — los servicios crean contexto bajo demanda con `_factory.CreateDbContextAsync()`. No se inyecta `DbContext` directamente.
- **Patrón de conexión**: MySQL a través de `Pomelo.EntityFrameworkCore.MySql`. `Server=localhost;User=root;Database=mecano` — sin autenticación de usuario con password (root local sin password).

---

## 6. Capa de servicios (lógica de negocio)

### Servicios registrados en DI (todos `Scoped`)

| Interfaz | Implementación | Firme de métodos | DI Life |
|---|---|---|---|
| `IClienteService` | `ClienteService` | `BuscarClientesAsync(termino, pagina, tamanioPagina)`<br>`ObtenerPorIdAsync(clienteId)`<br>`RegistrarAsync(cedula, nombre, telefono, correo)` | Scoped |
| `IVehiculoService` | `VehiculoService` | *(no leído completamente, ver sección de observaciones)* | Scoped |
| `IServicioService` | `ServicioService` | `ObtenerTodosActivosAsync()`<br>`ObtenerPorIdAsync(servicioId)`<br>`CalcularHoraFin(horaInicio, duracionMinutos)` | Scoped |
| `IMecanicoService` | `MecanicoService` | `ObtenerPorEspecialidadAsync(categoriaId)`<br>`ObtenerDisponiblesAsync(categoriaId, fecha, horaInicio, horaFin)` | Scoped |
| `ICitaService` | `CitaService` | `AgendarCitaAsync(dto)`<br>`CancelarCitaAsync(citaId)`<br>`ObtenerPorRangoAsync(inicio, fin)` | Scoped |
| `ICalendarQueryService` | `CalendarQueryService` | `ObtenerEventosAsync(inicio, fin)` | Scoped |
| `INotificationService` | `NotificationService` | `NotificarCitaAgendadaAsync(cita)` | Scoped |
| `IAuthServices` | `AuthServices` | `LoginAsync(email, password)`<br>`RegisterAdminAsync(email, nombre, password)`<br>`EmailExisteAsync(email)` | Scoped |
| `AuthenticationStateProvider` | `CustomAuthenticationStateProvider` | Revalida el estado de auth contra BD cada 10 min (hereda de `RevalidatingServerAuthenticationStateProvider`) | Scoped |
| — | `DbSeeder` | `SeedAsync()` — siembra categorías, servicios, mecánicos, clientes, vehículos, citas, admin global | Scoped |

### Servicios relacionados con usuarios/auth

**AuthServices** (`Logica/Servicios/AuthServices.cs:1-144`):
- **Patrón de hash**: PBKDF2 con `Rfc2898DeriveBytes`, SHA-256, 310,000 iteraciones, salt de 16 bytes (128 bits), hash de 32 bytes (256 bits).
- **Formato de salida**: `{saltBase64}${hashBase64}` (separado por `$`).
- **Verificación**: `CryptographicOperations.FixedTimeEquals` para resistencia a ataques de tiempo.
- **Login**: Intenta primero en `Administrador`, si no encuentra o contraseña inválida, intenta en `Mecanico` con filtro `m.Activo`.
- **RegisterAdmin**: Crea nuevo `Administrador` con `Activo = true` y hash de password.
- **EmailExiste**: Verifica si el email ya existe en tablas `Administradors` o `Mecanico`.
- **Inyección DI**: `AddScoped<IAuthServices, AuthServices>` en Program.cs:36.

### Servicios que usan `HttpContext` o `AuthenticationStateProvider`
- `CustomAuthenticationStateProvider` (`Logica/Servicios/CustomAuthenticationStateProvider.cs`) hereda de `RevalidatingServerAuthenticationStateProvider`; revalida cada 10 minutos contra BD que el usuario (admin o mecánico) siga `Activo`, evitando sesiones fantasma cuando se desactiva un usuario.
- Usa `IServiceScopeFactory` para crear un scope por validación (no mantiene un DbContext vivo durante todo el circuito Blazor).
- El `Error.razor` inyecta `HttpContext` cascading parameter, pero es para páginas de error, no para lógica de auth.

### Servicios que manejan sesión, cookies o tokens
- La cookie de autenticación la emite el endpoint `POST /api/auth/login` (`Endpoints/AuthEndpoints.cs`) vía `HttpContext.SignInAsync` con `AuthenticationProperties.IsPersistent = true` y expiración de 8 horas (con sliding expiration).
- `POST /api/auth/logout` la cierra con `SignOutAsync` y requiere autorización.
- El login desde la UI se hace con `window.authFetch.login` (`wwwroot/js/auth.js`) porque la cookie `Set-Cookie` debe llegar al navegador; si se usara `HttpClient` de Blazor Server, la cookie saldría del servidor hacia sí mismo y nunca llegaría al navegador.
- No se usan tokens JWT ni `ISession`.

---

## 7. Capa de UI (Blazor)

### Archivos Razor clave

#### `App.razor`
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <ResourcePreloader />
    <link rel="stylesheet" href="@Assets["lib/bootstrap/dist/css/bootstrap.min.css"]" />
    <link rel="stylesheet" href="@Assets["app.css"]" />
    <link rel="stylesheet" href="@Assets["Mecano.styles.css"]" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.13.1/font/bootstrap-icons.min.css">
    <ImportMap />
    <link rel="icon" type="image/png" href="favicon.png" />
    <HeadOutlet />
</head>
<body>
    <Routes />
    <ReconnectModal />
    <script src="@Assets["_framework/blazor.web.js"]"></script>
    <script src='https://cdn.jsdelivr.net/npm/fullcalendar@6.1.15/index.global.min.js'></script>
    <script src='js/fullcalendar-interop.js'></script>
</body>
</html>
```

#### `Routes.razor`
```razor
<Router AppAssembly="typeof(Program).Assembly" NotFoundPage="typeof(Pages.NotFound)">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)">
            <NotAuthorized>
                @if (context.User.Identity is null || !context.User.Identity.IsAuthenticated)
                {
                    <RedirectToLogin />
                }
                else
                {
                    <div>... alerta de "No tienes permisos" ...</div>
                }
            </NotAuthorized>
        </AuthorizeRouteView>
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

#### `_Imports.razor` (todos los using)
```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using Mecano
@using Mecano.Components
@using Mecano.Components.Layout
@using Mecano.Entidad.Clases
@using Mecano.Entidad.DTOs
@using Mecano.Logica.Interfaces
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
```

#### `MainLayout.razor`
```razor
@inherits LayoutComponentBase
<div class="page">
    <div class="sidebar">
        <NavMenu />
    </div>
    <main>
        <div class="top-row px-4">
            <a href="https://learn.microsoft.com/aspnet/core/" target="_blank">About</a>
        </div>
        <article class="content px-4">
            @Body
        </article>
    </main>
</div>
<div id="blazor-error-ui" data-nosnippet>
    An unhandled error has occurred.
    <a href="." class="reload">Reload</a>
    <span class="dismiss">🗙</span>
</div>
```

#### `NavMenu.razor`
```razor
<div class="top-row ps-3 navbar navbar-dark">
    <div class="container-fluid">
        <a class="navbar-brand" href="">Mecano</a>
    </div>
</div>

<input type="checkbox" title="Navigation menu" class="navbar-toggler" />

<div class="nav-scrollable" onclick="document.querySelector('.navbar-toggler').click()">
    <nav class="nav flex-column">
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="" Match="NavLinkMatch.All">
                <span class="bi bi-house-door-fill-nav-menu" aria-hidden="true"></span> Home
            </NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="connectiontest">
                <span class="bi bi-list-nested-nav-menu" aria-hidden="true"></span> TEST
            </NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="agendar">
                <span class="bi bi-list-nested-nav-menu" aria-hidden="true"></span> Agendar
            </NavLink>
        </div>
    </nav>
</div>
```

### Páginas `.razor` (lista completa)

| Ruta (`@page`) | Autorización | Propósito |
|---|---|---|
| `/` | `[Authorize]` | Página principal / home |
| `/counter` | `[Authorize]` | Contador de ejemplo (InteractiveServer) |
| `/weather` | `[Authorize]`, `[StreamRendering]` | Ejemplo de clima |
| `/agendar` | `[Authorize(Roles = "Administrador")]` | Formulario para agendar cita (InteractiveServer) |
| `/login` | `[AllowAnonymous]` | Login con EditForm + `window.authFetch` (InteractiveServer) |
| `/acceso-denegado` | `[AllowAnonymous]` | Página de acceso denegado |
| `/not-found` | Ninguna | Página de error 404 |
| `/Error` | Ninguna | Página de manejo de errores |
| `/connectiontest` | `[Authorize(Roles = "Administrador")]` | Prueba de conexión a la BD (InteractiveServer) |

### Componentes compartidos en `Shared/`
- `Components/Shared/RedirectToLogin.razor` — redirige a `/login` cuando `NotAuthorized` detecta usuario anónimo.
- Los componentes de layout están en `Components/Layout/`.

### Render mode configurado
- **Predominante**: `InteractiveServer` — la mayoría de páginas tienen `@rendermode InteractiveServer` (Counter, AgendarCita, ConnectionTest).
- `App.razor` usa `.AddInteractiveServerRenderMode()` en Program.cs.
- No hay configuración `WebAuto` o `WebAssembly` explícita más allá del server.

### Página de login, register o account
- **Login implementado**: `Components/Pages/Login.razor` (`@page "/login"`, InteractiveServer, `[AllowAnonymous]`) con `EditForm` y `DataAnnotationsValidator` sobre `LoginDTO`. Al autenticarse correctamente hace `NavigateTo("/", forceLoad: true)`.
- **No existe página de registro de usuarios**. `AuthServices.RegisterAdminAsync` existe en la capa lógica pero no tiene UI.
- Las credenciales de `Mecanico` se siembran vía `DbSeeder` (password por defecto `Mecanico123*`, email `<nombre>@mecano.cr`).

---

## 8. Estado actual de autenticación/autorización

### `AddAuthentication` / `AddAuthorization` en `Program.cs`
- **Presente**: `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...)` con `LoginPath = "/login"` y `AccessDeniedPath = "/acceso-denegado"`, cookie HttpOnly, SecurePolicy Always, SameSite Lax, expiración 8h con sliding expiration.
- `AddAuthorization` con dos políticas: `"Administrador"` (rol Administrador) y `"AdminGlobal"` (rol Administrador + claim `EsAdminGlobal=true`).

### `AuthenticationStateProvider` custom
- `CustomAuthenticationStateProvider` hereda de `RevalidatingServerAuthenticationStateProvider`; revalida contra BD cada 10 minutos que el usuario siga activo. Los usuarios anónimos se tratan como estado válido (retorna `true`); la redirección la hace `AuthorizeRouteView`.

### Claims / cookies en uso
- Claims emitidos en login (`AuthEndpoints.cs`): `ClaimTypes.NameIdentifier`, `ClaimTypes.Name`, `ClaimTypes.Email`, `ClaimTypes.Role`, y `EsAdminGlobal=true` solo si aplica.
- Cookie persistente de 8 horas; sin JWT ni `ISession`.

### `UseAuthentication()` / `UseAuthorization()`
- Presentes en el pipeline, después de `app.UseAntiforgery()` y antes de `MapAuthEndpoints()`.

### Atributos `[Authorize]` en el código
- `[Authorize]` en `Home.razor`, `Counter.razor`, `Weather.razor`.
- `[Authorize(Roles = Roles.Administrador)]` en `AgendarCita.razor` y `ConnectionTest.razor`.
- `[AllowAnonymous]` en `Login.razor` y `AccesoDenegado.razor`.

### `AuthorizeView` / `AuthorizeRouteView` en uso
- `Routes.razor` usa `<AuthorizeRouteView>` con plantilla `NotAuthorized`: si el usuario es anónimo, muestra `<RedirectToLogin />`; si está autenticado pero sin permisos, muestra alerta "No tienes permisos".

---

## 9. Modelo de dominio — Roles y permisos

### Entidades con propiedades de identidad

| Entidad | Email | HashPassword | Activo | EsAdminGlobal | Comentario |
|---|---|---|---|---|---|
| **Administrador** | ✅ Required, max 200 | ✅ Required, string vacío default | ✅ default true | ✅ bool — "Administradores son los ÚNICOS usuarios que pueden crear, reprogramar o cancelar citas" | Tiene acceso total |
| **Mecanico** | ✅ Required, max 200 | ✅ Required, string vacío default | ✅ default true | ❌ No tiene la propiedad | Tiene especialidad (Categoria) y gestiona citas |
| **Cliente** | ❌ No es Required (es `Correo`, opcional) | ❌ No tiene HashPassword | ✅ default true | N/A | Según resumen: "Clients do NOT have login accounts" |

### Algoritmo de hash utilizado
- **PBKDF2** (Password-Based Key Derivation Function 2), identificable en `AuthServices.cs:38`: `new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256)`.
- **Parámetros**: 310,000 iteraciones, salt de 16 bytes, hash de 32 bytes, SHA-256.
- **Formato de stored password**: `{saltBase64}${hashBase64}` (ej. `odU4QXZ3W2l0aWQ9...`).

### Constantes de roles definidas
- **Constante `SALTSIZE = 16`** y **`HASHSIZE = 32`** en `AuthServices.cs:13-14`.
- **Constante `Iterations = 310000`** en `AuthServices.cs:15`.
- **Clase estática `Roles`** en `Entidad/Constantes/Roles.cs` con `Administrador` y `Mecanico`, usada en `Program.cs` (políticas) y en atributos `[Authorize(Roles = ...)]`.

### Tabla de "qué hace cada rol" (deducción del código)

| Rol | Lo que puede hacer (basado en comentarios XML + atributos/servicios) |
|---|---|
| **Administrador** | - Único rol que puede crear/reeschedular/cancelar citas (comentario en `Administrador.cs:6-8`).<br>- Puede registrar nuevos administradores via `RegisterAdminAsync`.<br>- Acceso a todas las entidades en BD (tabla `Administradors`).<br>--- | |
| **Mecanico** | - Puede ver citas disponibles por especialidad y horario.<br>- No puede agendar si está inactivo (`InactiveMechanicException`).<br>- Puede tener citas asignadas (`Mecanico.Citas` navigation).<br>- Login requiere `m.Activo` (AuthServices:104).<br>- No tiene permisos de administración pura. | |

**Ya implementado**: El middleware ASP.NET Core (cookies + claims + `AuthorizeRouteView` + políticas) protege rutas por rol. Los roles se distinguen en el login por el tipo de entidad encontrado (`Administrador` vs `Mecanico` activo), y `AuthenticatedUser` ahora incluye `esAdminGlobal` como quinto campo del record.

---

## 10. Convenciones del proyecto

| Convención | Observación |
|---|---|
| **Espaciado de nombres** | `Mecano` (raíz), `Mecano.Entidad.Clases`, `Mecano.Entidad.DTOs`, `Mecano.Data`, `Mecano.Logica.Interfaces`, `Mecano.Logica.Servicios`, `Mecano.Components`, `Mecano.Components.Layout` |
| **Idioma de identificadores** | Mixto: nombres de clase y propiedades en **inglés** (Email, Password, Nombre, Categoria, Service, etc.), pero comentarios y mensajes de usuario en **español** (errores, placeholders, alerts). |
| **Idioma de comentarios** | Principalmente **español** (resumen de clases, comentarios XML), con algunos bloques de código en inglés (`using System.ComponentModel.DataAnnotations`). |
| **Patrón de nombres para archivos `.razor`** | `PascalCase` para nombres de archivo (Home.razor, Counter.razor, AgendarCita.razor) y `Razor` directives mezclados. Las rutas `@page` usan minúsculas con guiones (`/agendar`, `/connectiontest`). |
| **Nullable reference types** | `<Nullable>enable</Nullable>` en csproj — habilitado globalmente. Las entidades usan `string?` y `int?` donde es apropiado. |
| **Uso de `ImplicitUsings`** | `<ImplicitUsings>enable</ImplicitUsings>` — las directivas `global using` se generan automáticamente por el SDK. Los using explícitos en `_Imports.razor` y archivos individuales son redundantes pero presentes por costumbre. |

---

## 11. Riesgos y observaciones (solo hechos)

| Riesgo/Observación | Detalle |
|---|---|
| **Archivos muy grandes** | `AgendarCita.razor` tiene 462 líneas — es el archivo más grande del proyecto. |
| **Código duplicado evidente** | El patrón de `using var context = await _factory.CreateDbContextAsync();` y `VerifyPassword`/`HashPassword` repetitivo en `AuthServices`. Los métodos de servicio siguen patrón similar (crear contexto, consultar, guardar). |
| **TODOs o `HACK` en el código** | Ningún `TODO` o `HACK` literal encontrado, pero el `catch` genérico en `AuthServices.VerifyPassword` (línea 72-75) que retorna `false` en caso de cualquier excepción podría considerarse un patrón riesgoso. |
| **Comentarios que contradicen el código actual** | El comentario en `Cliente.cs:7` dice "Clients do NOT have login accounts" — lo cual es coherente con que `Cliente` no tenga `HashPassword`. Sin embargo, `AuthServices.LoginAsync` intenta login en `Mecanico` y `Administrador` solo, lo cual es consistente. |
| **Dependencias circulares entre proyectos** | No hay múltiples proyectos — riesgo de circulares no aplica en la actualidad. |
| **Paquetes NuGet desactualizados con vulnerabilidades** | `Pomelo.EntityFrameworkCore.MySql` versión 9.0.0 vs .NET 10.0 release. No hay evidencia de vulnerabilidades conocidas en las versiones usadas, pero es el primer release de EF Core 9/10 con este proveedor. |

---

## 12. Archivos clave (contenido literal)

Los siguientes archivos fueron incluidos con su contenido completo en las secciones anteriores. Esta sección los resume para referencia rápida:

| Archivo | Sección |
|---|---|
| `Mecano/Program.cs` | Sección 4 (Configuración y arranque) |
| `Mecano/Data/MySQLDBContext.cs` | Sección 5 (Capa de datos) |
| `Mecano/Entidad/Clases/Administrador.cs` | Sección 9 (Modelo de dominio) |
| `Mecano/Entidad/Clases/Mecanico.cs` | Sección 9 |
| `Mecano/Entidad/Clases/Cliente.cs` | Sección 9 |
| `Mecano/Entidad/Clases/Vehiculo.cs` | Sección 9 |
| `Mecano/Entidad/Clases/Servicio.cs` | Sección 9 |
| `Mecano/Entidad/Clases/Categoria.cs` | Sección 9 |
| `Mecano/Logica/Servicios/AuthServices.cs` | Sección 6 |
| `Mecano/Logica/Interfaces/IAuthServices.cs` | Sección 6 |
| `Mecano/Entidad/DTOs/AuthenticatedUser.cs` | Sección 6 |
| `Mecano/Entidad/DTOs/LoginDTO.cs` | Sección 6 |
| `Mecano/Entidad/DTOs/RegistroAdminDTO.cs` | Sección 6 |
| `Mecano/Components/App.razor` | Sección 7 |
| `Mecano/Components/Routes.razor` | Sección 7 |
| `Mecano/Components/_Imports.razor` | Sección 7 |
| `Mecano/Components/Layout/MainLayout.razor` | Sección 7 |
| `Mecano/Components/Pages/Login.razor` + `Login.razor.cs` | Sección 7 |
| `Mecano/Components/Pages/AccesoDenegado.razor` | Sección 7 |
| `Mecano/Components/Shared/RedirectToLogin.razor` | Sección 7 |
| `Mecano/Endpoints/AuthEndpoints.cs` | Sección 8 |
| `Mecano/Logica/Servicios/CustomAuthenticationStateProvider.cs` | Secciones 6 y 8 |
| `Mecano/Data/DbSeeder.cs` | Secciones 4 y 5 |
| `Mecano/Entidad/Constantes/Roles.cs` | Sección 9 |
| `Mecano/wwwroot/js/auth.js` | Sección 8 |
| `Mecano/Components/Layout/NavMenu.razor` | Sección 7 |
| `Mecano/Entidad/Excepciones/InactiveMechanicException.cs` | Riesgos 11 |
| `Mecano/Entidad/Exceptions/AppointmentOverlapException.cs` | Riesgos 11 |

---

## 13. Preguntas abiertas para el implementador

1. **¿Cómo se crea el primer usuario si no hay seed?** — Resuelto: `DbSeeder` crea/actualiza un Administrador Global en cada arranque (`adminglobal@mecano.cr` / `AdminGlobal123*` por defecto, sobreescribible con `Seed:GlobalAdmin:Email`/`Password`).

2. **¿Se requiere autenticación cookie/session en el pipeline ASP.NET Core?** — Resuelto: ya está implementada (cookies + `UseAuthentication`/`UseAuthorization` + `AuthorizeRouteView` + políticas).

3. **¿Cómo se distingue entre un `Administrador` y un `Mecanico` al login?** — `AuthServices.LoginAsync` intenta primero `Administrador` y luego `Mecanico` activo; el rol se codifica en el claim `ClaimTypes.Role`.

4. **¿Qué ocurre si un `Mecánico` es desactivado (`Activo = false`) mientras tiene sesión activa?** — Resuelto: `CustomAuthenticationStateProvider` revalida contra BD cada 10 minutos y retorna `false` si el usuario fue desactivado/eliminado, lo que cierra la sesión del circuito.

5. **¿Hay rate limiting en las endpoints de login/register?** — Sigue sin haber rate limiting visible; PBKDF2 mitiga fuerza bruta a nivel de hash pero no hay límite de intentos a nivel de endpoint.

6. **¿Se requiere soporte para múltiples idiomas?** — Sigue abierto: UI y mensajes mezclan español/inglés.

7. **¿Cómo se manejan los tokens de actualización o "remember me"?** — No hay `RememberMe` ni refresco; la cookie expira a las 8 horas con sliding expiration.

8. **¿La propiedad `EsAdminGlobal` se usa en alguna lógica?** — Resuelto: se emite como claim `EsAdminGlobal=true` en el login y existe la política `"AdminGlobal"` que la exige.

9. **¿Existe alguna política de contraseña compleja más allá de los 6 caracteres minimum?** — Sigue abierto: `LoginDTO`/`RegistroAdminDTO` validan formato, pero no se observó validación de fortaleza en `AuthServices.RegisterAdminAsync`.

10. **¿Cómo se persiste el estado `AuthenticatedUser` entre requests?** — Resuelto: ya no se usa el record en memoria para auth; la sesión vive en la cookie y el estado Blazor se reconstruye vía `CustomAuthenticationStateProvider` (claims del `ClaimsPrincipal`).