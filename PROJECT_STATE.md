# PROJECT STATE: Mecano

## 1. Resumen ejecutivo

| Campo | Valor |
|---|---|
| **Nombre del proyecto** | Mecano |
| **Propósito** | Aplicación de taller mecánico para agendar citas, gestionar clientes, vehículos, servicios y mecánicos. |
| **Stack principal** | .NET 10.0 (SDK Web), Blazor Server (Interactive Server), Entity Framework Core 9.0, MySQL 8.0 (Pomelo EF Provider) |
| **Estado general** | Prototipo / MVP — gestión operativa de citas, clientes, vehículos, categorías y servicios, con autenticación por cookies y autorización por roles implementadas. |
| **Páginas Blazor** | 12 páginas (.razor) entre Components/Pages y Components/Pages/{Categoria,Clientes,Mecanicos}; además Components/Shared/RedirectToLogin.razor y Layout (MainLayout, NavMenu, ReconnectModal). Estilo global "Industrial Clean" con variables CSS light/dark (`wwwroot/app.css` + `wwwroot/js/theme.js`). |
| **Entidades de dominio** | 8 clases principales: Administrador, Mecanico, Cliente, Vehiculo, Servicio, Categoria, Cita, NotificacionLog |
| **Invariante de datos** | `Cliente.CedulaIdentidad` (9 dígitos), `Cliente.Telefono` (8 dígitos, opcional) y `Vehiculo.Placa` (6 caracteres alfanuméricos, **mayúsculas y sin guiones**, ej. `ABC123`) se persisten **solo con la forma canónica**. El formato legible se aplica en la capa de presentación vía `Entidad/Utilidades/Formatos.cs`. Ver Sección 5.1. Los precios (`Servicio.Precio`) se presentan como colones vía `Formatos.Moneda` (cultura es-CR fija). |
| ** Keep “YAGNI” and “KISS” in mind ** | I don't want a project with a super-complex backend
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
│  │  ├─ Categoria.cs
│  │  └─ NotificacionLog.cs
│  ├─ DTOs/                  # Objetos de transferencia de datos
│  │  ├─ AuthenticatedUser.cs
│  │  ├─ LoginDTO.cs
│  │  ├─ RegistroAdminDTO.cs
│  │  ├─ AgendarCitaDTO.cs
│  │  ├─ HorarioDisponibleDTO.cs
│  │  ├─ CalendarEventDTO.cs
│  │  ├─ CitaDTO.cs            # Proyección de Cita (servicio, mecánico, placa)
│  │  ├─ NuevoVehiculoDTO.cs    # Incluye ClienteId; DataAnnotations (placa laxo a propósito, regla estricta en el servicio)
│  │  ├─ ActualizarVehiculoDTO.cs # Write model con DataAnnotations, plano (misma convención que los DTOs de Servicio)
│  │  ├─ ServicioDTO.cs        # Read model de Servicio
│  │  ├─ CrearServicioDTO.cs    # Write model con DataAnnotations
│  │  ├─ ActualizarServicioDTO.cs
│  │  ├─ Cliente/
│  │  │  ├─ ClienteDTO.cs        # Read model; TipoIdentificacion solo lectura
│  │  │  ├─ CrearClienteDTO.cs   # Sin campo de tipo: el servicio asigna Nacional
│  │  │  └─ ActualizarClienteDTO.cs
│  │  ├─ Categoria/
│  │     ├─ CategoriaDTO.cs
│  │     ├─ CrearCategoriaDTO.cs
│  │     └─ ActualizarCategoriaDTO.cs
│  │  └─ Mecanico/             # Namespace que colisiona con la clase Mecanico (ver Sección 11)
│  │     ├─ MecanicoDTO.cs       # Read model; EspecialidadNombre resuelto desde Categoria
│  │     ├─ CrearMecanicoDTO.cs  # Write model (Password + ConfirmarPassword con [Compare])
│  │     └─ ActualizarMecanicoDTO.cs # Password opcional (vacío = conservar hash)
│  ├─ Excepciones/
│  │  ├─ InactiveMechanicException.cs
│  │  └─ AppointmentOverlapException.cs
│  ├─ Utilidades/
│  │  └─ Formatos.cs        # Normalización y formateo de cédula/teléfono/placa/moneda
│  └─ Constantes/
│     ├─ Roles.cs            # Strings centralizados: "Administrador", "Mecanico"
│     └─ TipoIdentificacion.cs # Enum: Nacional=1, Dimex=2, Pasaporte=3 (Fase 1 solo Nacional)
├─ Data/
│  ├─ MySQLDBContext.cs     # DbContext con DbSet<>
│  ├─ DbSeeder.cs           # Seed de datos iniciales (categorías, servicios, mecánicos, etc.)
│  └─ Migrations/           # 20261003033206_Inicial + 20261005232947_AgregarTipoIdentificacionCliente + 20261006160916_Vehiculo-Activo-bool
├─ Endpoints/
│  └─ AuthEndpoints.cs      # Minimal API: POST /api/auth/login, POST /api/auth/logout
├─ Logica/
│  ├─ Interfaces/           # Firmas de servicio (I*Service, IAuthServices)
│  ├─ Utilidades/
│  │  └─ PasswordHasher.cs   # PBKDF2-SHA256 estático: fuente única del hash de contraseñas
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
├─ wwwroot/
│  ├─ app.css               # Tokens CSS "Industrial Clean" (light/dark) + estilos globales
│  └─ js/
│     ├─ auth.js               # window.authFetch.login/logout (fetch con credentials same-origin)
│     ├─ fullcalendar-interop.js
│     └─ theme.js              # window.mecanoTheme: toggle light/dark + persistencia en localStorage
├─ Components/
│  ├─ _Imports.razor          # Directivas using compartidas
│  ├─ Routes.razor           # Configuración del enrutador
│  ├─ App.razor              # Raíz HTML con script de framework Blazor
│  ├─ Layout/
│  │  ├─ MainLayout.razor    # Top-header "Panel de Control" + toggle de tema (light/dark) + sidebar + @Body (canvas px-4 py-3)
│  │  ├─ MainLayout.razor.css # Sidebar con var(--bg-sidebar); header con var(--bg-header) y var(--border-color)
│  │  ├─ NavMenu.razor       # Brand MECANO; Inicio, Agendar Cita; sección "Gestión" (AuthorizeView Administrador): Clientes y Vehículos, Categorías y Servicios, Mecánicos; TEST al final
│  │  ├─ NavMenu.razor.css    # Links con tokens (--text-sidebar, --nav-active-bg, --nav-hover-bg, --primary-accent)
│  │  └─ ReconnectModal.razor
│  ├─ Pages/
│     ├─ Home.razor          # @page "/", [Authorize]
│     ├─ AgendarCita.razor  # @page "/agendar", [Authorize(Roles=Administrador)], InteractiveServer
│     ├─ Login.razor         # @page "/login", [AllowAnonymous], InteractiveServer
│     ├─ AccesoDenegado.razor # @page "/acceso-denegado", [AllowAnonymous]
│     ├─ NotFound.razor      # @page "/not-found"
│     ├─ Error.razor         # @page "/Error"
│     ├─ ConnectionTest.razor # @page "/connectiontest", [Authorize(Roles=Administrador)], InteractiveServer
│     ├─ Categoria/
│     │  ├─ Categorias.razor      # @page "/categorias", lista + CRUD modal (Administrador)
│     │  ├─ Categorias.razor.cs
│     │  ├─ CategoriaDetalle.razor # @page "/categorias/{Id:int}", servicios con CRUD embebido
│     │  └─ CategoriaDetalle.razor.cs
│     ├─ Clientes/               # Namespace plural: 'Cliente' colisiona con la entidad
│     │  ├─ Clientes.razor        # @page "/clientes", [Authorize] lectura; escritura vía AuthorizeView
│     │  ├─ Clientes.razor.cs     # búsqueda con debounce + paginación numerada
│     │  ├─ ClienteDetalle.razor  # @page "/clientes/{Id:int}", [Authorize], CRUD de vehículos embebido (modal para Administrador)
│     │  └─ ClienteDetalle.razor.cs
│     └─ Mecanicos/              # Namespace plural por la misma razón (colisión con la clase Mecanico)
│        ├─ Mecanicos.razor      # @page "/mecanicos", [Authorize(Roles=Administrador)], lista + modal crear/editar + desactivar/reactivar
│        └─ Mecanicos.razor.cs
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

            // Unique index compuesta: la identidad solo es única dentro de su tipo de documento.
            // Fase 1 solo usa Nacional, pero el índice compuesto evita migrar en Fase 2.
            modelBuilder.Entity<Cliente>()
                .HasIndex(c => new { c.CedulaIdentidad, c.TipoIdentificacion })
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
| Vehiculo | `DbSet<Vehiculo>` | Pertenece a un Cliente; soft delete vía `Activo`; placa única normalizada (ABC123) |

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
1. `Cliente (CedulaIdentidad, TipoIdentificacion)` — Unique index compuesto.
2. `Vehiculo.Placa` — Unique index.

### Estado de migraciones
- **Carpeta `Data/Migrations/`**: Tres migraciones — `20261003033206_Inicial.cs`, `20261005232947_AgregarTipoIdentificacionCliente.cs` y `20261006160916_Vehiculo-Activo-bool.cs` (cada una con su `.Designer.cs`, más `MySQLDBContextModelSnapshot.cs`).
- **¿Migraciones aplicadas a la BD?**: El seeder usa `Database.EnsureCreatedAsync()` en lugar de `MigrateAsync()`, por lo que el esquema se crea directamente desde el modelo; las migraciones existen como historial pero no se aplican en runtime. `EnsureCreatedAsync` es no-op sobre una BD ya existente, así que **agregar una columna requiere borrar la BD local** (`DROP DATABASE mecano;`) y dejar que el seeder la reconstruya.
- **`TipoIdentificacion` en Cliente**: Columna `int NOT NULL DEFAULT 1`. EF no puede deducir el default porque el enum arranca en `Nacional = 1` (sin miembro `0`), por lo que el `defaultValue` está fijado a mano en la migración; sin él las filas existentes quedarían en `0`, un valor inválido.
- **Índice único de Cliente**: Compuesto `(CedulaIdentidad, TipoIdentificacion)`. El índice único de `CedulaIdentidad` por sí solo se eliminó para permitir que DIMEX y Pasaporte convivan con un Nacional en la Fase 2.
- **`Vehiculo.Activo`**: Columna `bool` agregada por `20261006160916` **sin `defaultValue`** (el modelo EF no define `HasDefaultValue`). La entidad no lleva inicializador `= true`; cada punto de creación asigna explícito `Activo = true`: `VehiculoService.CrearAsync`, `CitaService.AgendarCitaAsync` y `DbSeeder`. No existe `ReactivarAsync` de vehículo — un vehículo desactivado desaparece de listas y selects, y se restaura por caminos laterales con UI: **transferirlo** (siempre guarda `Activo = true`) o **reactivar su cliente** (cascada). Ver Sección 11.
- **`Vehiculo.Placa`**: única (índice), almacenada normalizada (`ABC123`). El chequeo de duplicado debe mirar **todas** las filas, incluidas las inactivas (el índice único no filtra por `Activo`): si no, la colisión estallaría como `DbUpdateException` cruda. Ver Sección 5.1.
- **Estrategia de acceso a BD**: `IDbContextFactory<MySQLDBContext>` — los servicios crean contexto bajo demanda con `_factory.CreateDbContextAsync()`. No se inyecta `DbContext` directamente.
- **Normalización y formato de cédula/teléfono/placa**: ver Sección 5.1.

### 5.1 Normalización y formato de identificadores de Cliente y Vehiculo, y formato de moneda

**El problema.** `Cliente.CedulaIdentidad` estaba indexada como cadena cruda. Un usuario que escribía `101110111` y otro que escribía `1-0111-0111` generaban dos filas para la misma persona, porque `"101110111" != "1-0111-0111"` para el índice único `(CedulaIdentidad, TipoIdentificacion)`. En paralelo, el DTO solo validaba `[StringLength(20)]`, así que entradas como `120adsa91028fmask` se aceptaban y guardaban tal cual.

**El invariante.** `CedulaIdentidad` y `Telefono` se guardan únicamente con dígitos, sin guiones ni espacios. `Vehiculo.Placa` se guarda en mayúsculas ASCII y solo alfanuméricos, sin guiones (`ABC123`). El formato con guiones es exclusivamente de presentación.

**`Entidad/Utilidades/Formatos.cs`** — helper estático, namespace `Mecano.Entidad.Utilidades`:

| Método | Comportamiento |
|---|---|
| `SoloDigitos(raw)` | Descarta todo carácter que no sea ASCII `0-9`. Usa `c is >= '0' and <= '9'` en vez de `char.IsDigit` a propósito: `char.IsDigit` acepta dígitos Unicode (ej. `١٢٣` en arábigo-indic) que no corresponden a una cédula costarricense. Devuelve `string.Empty` ante null. |
| `CedulaNacional(raw)` | `101110111` → `1-0111-0111` (1-4-4). Solo formatea si hay exactamente 9 dígitos; ante cualquier otro largo devuelve el input **sin modificar**, para no imprimir guiones en posiciones que no corresponden. |
| `Telefono(raw)` | `88888888` → `8888-8888` (4-4). Misma regla de largo. |
| `NormalizarPlaca(raw)` | `"abc-123"` → `"ABC123"`: mayúsculas ASCII, descarta todo lo que no sea `0-9`/`A-Z`/`a-z`. Es la forma de almacenamiento de `Vehiculo.Placa`; pasa antes del chequeo de duplicado y del guardado (invariante de `VehiculoService` y de la creación rápida de `CitaService`). |
| `Placa(raw)` | `"ABC123"` → `"ABC-123"` (3-3). Solo formatea si el normalizado tiene exactamente 6 caracteres; ante otro largo devuelve el input sin modificar. Misma regla que `CedulaNacional` ante entradas inesperadas. |
| `Moneda(monto)` | `35000` → `₡35 000,00`. Formato de colón con **cultura `es-CR` fija** (campo `static readonly CultureInfo EsCr`, no depende de la cultura del circuito/servidor): símbolo `₡` U+20A1, coma decimal, espacio no-cortable U+00A0 como separador de miles, siempre 2 decimales (`C2`). Solo presentación — nunca para parsear ni persistir. |

Constantes `LargoCedulaNacional = 9`, `LargoTelefono = 8` y `LargoPlaca = 6`, para que el formateo y la validación no puedan divergir. Los 9 dígitos de una cédula nunca pueden interpretarse como teléfono, y una placa alfanumérica nunca colisiona con ninguno de los dos formateadores, así que las tres reglas no entran en conflicto.

**Orden de operaciones en `ClienteService`.** `CrearAsync` y `ActualizarAsync` ejecutan `NormalizarYValidar` (privado, estático) **antes** de `ExisteIdentificacionAsync`. El orden importa: la comprobación de duplicado y el guardado deben operar sobre la misma forma del valor que realmente se persiste. Antes de este cambio el chequeo de duplicado corría sobre el string crudo mientras se guardaba el `.Trim()`, sin normalizar.

`NormalizarYValidar` lanza `InvalidOperationException` en Spanish al violar el largo. El teléfono es opcional: si queda vacío o es solo basura se guarda `null` y no se valida; si viene con dígitos, deben ser exactamente 8. `VehiculoService` tiene su propio `NormalizarYValidar` privado (placa normalizada de exactamente `LargoPlaca` y `Anio` en 1900–2100) que corre antes del chequeo de duplicado y del guardado; el chequeo debe ver **todas** las filas —incluidas las desactivadas— porque el índice único de `Vehiculo.Placa` es global. La creación rápida de vehículo en `CitaService.AgendarCitaAsync` normaliza la placa (y asigna `Activo = true`) pero **no** chequea duplicados: deuda aceptada (ver Sección 11).

**DTOs laxos, servicio estricto (misma decisión para placa y cédula).** `NuevoVehiculoDTO`/`ActualizarVehiculoDTO` aceptan placas de hasta 20 caracteres a propósito (`"ABC-123"` ocupa 7); la regla estricta de 6 viven solo en `VehiculoService.NormalizarYValidar`, de modo que el error llegue a la UI por el `catch (InvalidOperationException)` que la página ya maneja. Si se agrega `[RegularExpression]` al DTO, el error pasaría a `ValidationMessage` y habría que duplicar la lógica en dos lugares.

**`ExisteIdentificacionAsync`** normaliza su propia entrada con `SoloDigitos` aunque ya reciba un valor normalizado (operación idempotente). El método es público y podría llamarse con un valor crudo, en cuyo caso compararía mal contra lo almacenado. Con la normalización aplicada, `101110111` y `1-0111-0111` colisionan correctamente y el segundo intento falla por cédula duplicada en vez de crear un registro nuevo. `RegistrarAsync` hereta todo el comportamiento por delegation a `CrearAsync`.

**Búsqueda.** `AplicarFiltro` compara el término de búsqueda de dos formas para la cédula: tal como lo escribió el usuario, y normalizado a dígitos (`1-0111` encuentra `101110111`). La placa funciona igual: se compara tal cual y normalizada (`ABC-123`, `abc123` y `ABC123` encuentran la fila `ABC123`). El nombre no se normaliza porque no es un campo alfanumérico canónico. Sin esto, una fila mostrada como `1-0111-0111` / `ABC-123` no sería buscable con ese mismo texto.

**Presentación.** `@using Mecano.Entidad.Utilidades` se agregó a `Components/_Imports.razor`, y el formateo se aplica en el markup de `Clientes.razor` (tabla y confirmación de borrado), `ClienteDetalle.razor` (datos y confirmación, más la tabla/modal de vehículos) y `AgendarCita.razor` (selector de cliente y de vehículo). Se prefirió llamar al helper desde el `.razor` antes que agregar getters computados a `ClienteDTO`: el DTO se puebla con `Select(Proyeccion)` en EF, y cualquier propiedad calculada quedaría sin valor porque EF solo asigna las propiedades mapeadas.

**Presentación de precios (colones).** `Formatos.Moneda` es el único punto de formato de moneda de la app (decisión: moneda fija CRC, cultura fija es-CR, sin configuración en `Program.cs` ni middleware de localización). Se aplica en los 3 sitios donde se muestra `Servicio.Precio`: la tabla de servicios de `CategoriaDetalle.razor:89`, el hint en vivo bajo el `InputNumber` de precio (`CategoriaDetalle.razor`, "Se mostrará como ₡35 000,00"), y las opciones del select de servicio de `AgendarCita.razor:181`. Antes mostraban `$35000` crudo con `$` hardcodeado. Verificado en runtime: `Formatos.Moneda(35000)` devuelve `₡35 000,00` incluso con `CurrentCulture = en-US`.

**Datos existentes.** No se agregó migración de normalización. La decisión fue tirar la base: el seeder ya usa `EnsureCreatedAsync`, que es no-op sobre una base existente, así que la reconstrucción era necesaria de todas formas. `DbSeeder` fue actualizado para insertar los tres clientes con dígitos (`101010101`/`88881111`, `202020202`/`88882222`, `303030303`/`88883333`) y los vehículos con placas normalizadas y `Activo = true` (`ABC123`, `DEF456`, `GHI789`, `JKL012`, `MNO345`), respetando el invariante desde el arranque.

**Fuera de alcance.** `Mecanico.Cedula` sigue almacenando guiones (`"3-3333-3333"`). Es otra entidad, sin capa de normalización ni formateo asociado; quitarle los guiones lo volvería ilegible en crudo. `Mecano.Entidad.Utilidades.Formatos` puede reutilizarse para ese fin si más adelante se define la regla.
- **Patrón de conexión**: MySQL a través de `Pomelo.EntityFrameworkCore.MySql`. `Server=localhost;User=root;Database=mecano` — sin autenticación de usuario con password (root local sin password).

---

## 6. Capa de servicios (lógica de negocio)

### Servicios registrados en DI (todos `Scoped`)

| Interfaz | Implementación | Firme de métodos | DI Life |
|---|---|---|---|
| `IClienteService` | `ClienteService` | **Legado:** `BuscarClientesAsync(termino, pagina, tamanioPagina)` (devuelve `Cliente`, sin filtro de Activo)<br>`ObtenerPorIdAsync(clienteId)` (devuelve `Cliente`)<br>`RegistrarAsync(...)` (envoltura de `CrearAsync`, devuelve `int`)<br>**CRUD:** `BuscarClientesPaginadoAsync(termino, pagina, tamanioPagina, incluirInactivos)`<br>`ObtenerTodosAsync(soloActivos)`<br>`ObtenerDetallePorIdAsync(id)`<br>`CrearAsync(dto)` → `int`<br>`ActualizarAsync(dto)`<br>**Estado:** `DesactivarAsync(id)` (soft delete **en cascada**: los vehículos activos del cliente pasan a inactivos; una sola `SaveChangesAsync`)<br>`ReactivarAsync(id)` (**en cascada**: TODOS los vehículos del cliente vuelven a activos, sin flag `DesactivadoPorCascade`)<br>`EliminarDefinitivoAsync(id)` (hard delete, exige 0 vehículos y 0 citas)<br>**Helpers:** `ContarBusquedaAsync(termino, incluirInactivos)`<br>`ExisteIdentificacionAsync(cedula, excluirId)` | Scoped |
| `IVehiculoService` | `VehiculoService` | `ObtenerPorClienteAsync(clienteId, incluirInactivos = false)` — solo activos por defecto (el parámetro opcional protege al llamador legado de `AgendarCita`)<br>`CrearAsync(dto)` → `int`<br>`ActualizarAsync(dto)` → `bool`<br>`EliminarAsync(id)` → `bool` (soft delete: `Activo = false`; sin `ReactivarAsync`)<br>`TransferirAsync(vehiculoId, nuevoClienteId)` → `bool` — reasigna dueño, **reactiva** el vehículo y bloquea si tiene citas futuras | Scoped |
| `IServicioService` | `ServicioService` | `ObtenerTodosActivosAsync()`<br>`ObtenerPorIdAsync(servicioId)`<br>`CalcularHoraFin(horaInicio, duracionMinutos)`<br>`ObtenerTodosAsync(soloActivos = true)`<br>`ObtenerDetallePorIdAsync(id)`<br>`ObtenerPorCategoriaAsync(categoriaId, soloActivos = true)`<br>`CrearAsync(dto)`<br>`ActualizarAsync(dto)`<br>`EliminarAsync(id)` (soft delete)<br>`ReactivarAsync(id)`<br>`ExisteNombreEnCategoriaAsync(nombre, categoriaId, excluirId)` | Scoped |
| `ICategoriaService` | `CategoriaService` | `ObtenerTodasAsync()`<br>`ObtenerPorIdAsync(id)`<br>`CrearAsync(dto)`<br>`ActualizarAsync(dto)`<br>`EliminarAsync(id)`<br>`ExisteNombreAsync(nombre, excluirId)` | Scoped |
| `IMecanicoService` | `MecanicoService` | **Legado (consumido por AgendarCita):** `ObtenerPorEspecialidadAsync(categoriaId)`<br>`ObtenerDisponiblesAsync(categoriaId, fecha, horaInicio, horaFin)`<br>**CRUD admin (consumido por `Components/Pages/Mecanicos/Mecanicos.razor`):** `ObtenerTodosAsync(soloActivos = true)`<br>`ObtenerDetallePorIdAsync(id)`<br>`CrearAsync(dto)` → `int`<br>`ActualizarAsync(dto)` → `bool`<br>`DesactivarAsync(id)`<br>`ReactivarAsync(id)`<br>`ExisteEmailAsync(email, excluirId)` | Scoped |
| `ICitaService` | `CitaService` | `AgendarCitaAsync(dto)`<br>`CancelarCitaAsync(citaId)`<br>`ObtenerPorRangoAsync(inicio, fin)`<br>`ObtenerPorClienteAsync(clienteId)` → `List<CitaDTO>` | Scoped |
| `ICalendarQueryService` | `CalendarQueryService` | `ObtenerEventosAsync(inicio, fin)` | Scoped |
| `INotificationService` | `NotificationService` | `NotificarCitaAgendadaAsync(cita)` | Scoped |
| `IAuthServices` | `AuthServices` | `LoginAsync(email, password)`<br>`RegisterAdminAsync(email, nombre, password)`<br>`EmailExisteAsync(email)` | Scoped |
| `AuthenticationStateProvider` | `CustomAuthenticationStateProvider` | Revalida el estado de auth contra BD cada 10 min (hereda de `RevalidatingServerAuthenticationStateProvider`) | Scoped |
| — | `DbSeeder` | `SeedAsync()` — siembra categorías, servicios, mecánicos, clientes, vehículos, citas, admin global | Scoped |

**Notas de comportamiento relevantes:**
- `ClienteDTO.CantidadVehiculos` es **condicional en la proyección**: cliente activo → cuenta solo vehículos `Activo = true`; cliente inactivo → cuenta **todos** los vehículos (coincide con la lista visible de `/clientes/{id}`, que usa `incluirInactivos: !cliente.Activo`). La proyección `c.Activo ? c.Vehiculos.Count(v => v.Activo) : c.Vehiculos.Count()` fue **verificada contra MySQL real**: EF Core 9 la traduce a SQL sin errores. El guard de `EliminarDefinitivoAsync` sigue contando **todos** los vehículos, así que un cliente inactivo con solo vehículos desactivados y 0 citas ahora **no** muestra el botón Eliminar (el conteo visible = total) — dirección segura.
- **Cascada de estado de vehículos**: `DesactivarAsync` desactiva solo los vehículos activos; `ReactivarAsync` reactiva **todos** (incluidos los que se habían desactivado individualmente antes). Sin flag `DesactivadoPorCascade` (YAGNI). En ambos casos las entidades están tracked y hay una sola `SaveChangesAsync` (transacción implícita de EF).
- **Transferencia de vehículos** (`TransferirAsync`): guards en orden — vehículo nulo → `false`; mismo dueño → `InvalidOperationException("El vehículo ya pertenece a este cliente.")`; destino nulo/inactivo → `"El cliente destino no está activo."`; citas futuras (`Fecha >= hoy`, estado no Cancelada/Finalizada) → `"El vehículo tiene N citas futuras. Cancelelas antes de transferirlo."`. Al guardar: `ClienteId = nuevo` + `Activo = true` (la transferencia reactiva). Las citas pasadas conservan el `ClienteId` anterior.
- El botón Eliminar de vehículo vive en el footer del modal de edición (no hay botones de acción por encima de lo heredado en la tabla); el confirm de borrado reemplaza al formulario sin apilar modales, e igual hace el botón **Transferir** (cierra el formulario y abre el modal de transferencia en su lugar). `VehiculoService` usa `NormalizarYValidar` + duplicado sobre todas las filas, patrón espejo de `ClienteService`.
- La advertencia de citas futuras al desactivar se cuenta **client-side**: `ClienteDetalle` usa la lista `citas` ya cargada; `Clientes` hace un `CitaService.ObtenerPorClienteAsync` al abrir el modal (con `catch` que silencia la advertencia si el conteo falla).
- **`PasswordHasher` estático** (`Logica/Utilidades/PasswordHasher.cs`): PBKDF2-SHA256, 310k iteraciones, salt 16B, hash 32B, formato `{saltBase64}${hashBase64}`. Fuente única de verdad: `AuthServices.HashPassword` y `VerifyPassword` delegan en él (la extracción usa el método estático `Rfc2898DeriveBytes.Pbkdf2`, por lo que las 3 advertencias SYSLIB0060 del código anterior desaparecieron). `DbSeeder` sigue usando `AuthServices.HashPassword`. `VerifyPassword` conserva la compat legado: un hash sin `$` se trata como texto plano y loggea warning.
- **CRUD de mecánicos (backend)**: `MecanicoService.CrearAsync`/`ActualizarAsync` normalizan email (trim + `ToLowerInvariant`, invariante: siempre minúsculas), verifican unicidad **manual** contra `Administradors` y `Mecanico` (`ExisteEmailAsync`; no hay índice único en la tabla), exigen que la especialidad exista, exigen `Password` mín. 6 + `[Compare]` en Crear, y en Actualizar `Password` vacío = conservar hash / parcialmente lleno = `InvalidOperationException`. `Cedula`/`Telefono` solo reciben Trim (los guiones se conservan — invariante intencional distinto a `Cliente`). `DesactivarAsync` **no revisa citas futuras** (ver Sección 11).

### Servicios relacionados con usuarios/auth

**AuthServices** (`Logica/Servicios/AuthServices.cs`):
- **Patrón de hash**: PBKDF2-SHA256 vía `PasswordHasher` (`Logica/Utilidades/PasswordHasher.cs`), 310,000 iteraciones, salt de 16 bytes (128 bits), hash de 32 bytes (256 bits); formato `{saltBase64}${hashBase64}`.
- **Formato de salida**: `{saltBase64}${hashBase64}` (separado por `$`).
- **Verificación**: `PasswordHasher.Verify` con `CryptographicOperations.FixedTimeEquals`; el caso legado de contraseña en texto plano (sin `$`) lo sigue manejando `VerifyPassword` con warning.
- **Login**: Intenta primero en `Administrador`, si no encuentra o contraseña inválida, intenta en `Mecanico` con filtro `m.Activo`.
- **RegisterAdmin**: Crea nuevo `Administrador` con `Activo = true` y hash de password.
- **EmailExiste**: Verifica si el email ya existe en tablas `Administradors` o `Mecanico` (email normalizado con `ToLowerInvariant`; compara `Mecanico.Email` directo porque es invariante minúsculas).
- **Inyección DI**: `AddScoped<IAuthServices, AuthServices>` en Program.cs.

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
    <script>
        (function () {
            try {
                var t = localStorage.getItem('mecano-theme');
                var dark = t ? t === 'dark' : window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
                if (dark) document.documentElement.setAttribute('data-bs-theme', 'dark');
            } catch (e) { }
        })();
    </script>
    <ImportMap />
    <link rel="icon" type="image/png" href="favicon.png" />
    <HeadOutlet />
</head>
<body>
    <Routes @rendermode="InteractiveServer" />
    <ReconnectModal />
    <script src="@Assets["_framework/blazor.web.js"]"></script>
    <script src='https://cdn.jsdelivr.net/npm/fullcalendar@6.1.15/index.global.min.js'></script>
    <script src='js/fullcalendar-interop.js'></script>
    <script src="js/auth.js"></script>
    <script src="js/theme.js"></script>
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
@using Mecano.Entidad.Utilidades
@using Mecano.Logica.Interfaces
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
```

#### `MainLayout.razor`
```razor
@inherits LayoutComponentBase
@inject IJSRuntime JS
@inject NavigationManager NavigationManager

<div class="page">
    <div class="sidebar">
        <NavMenu />
    </div>

    <main>
        <div class="top-row px-4">
            <div class="d-flex align-items-center gap-2">
                <i class="bi bi-wrench-adjustable fs-5 text-primary"></i>
                <h5 class="mb-0 fw-semibold">Panel de Control</h5>
            </div>

            <div class="d-flex align-items-center gap-2">
                <button class="theme-toggle-btn btn btn-sm btn-outline-secondary d-flex align-items-center justify-content-center"
                        type="button"
                        title="Cambiar tema oscuro/claro"
                        aria-label="Cambiar tema oscuro/claro"
                        @onclick="ToggleTheme">
                    <i class="bi theme-toggle-icon" data-on="bi-moon-stars" data-off="bi-sun"></i>
                </button>

                <AuthorizeView>
                    <Authorized>
                    <div class="dropdown"> ... menú de usuario (nombre, email, rol, logout vía HandleLogout) ... </div>
                    </Authorized>
                    <NotAuthorized> ... botón "Iniciar Sesión" -> /login ... </NotAuthorized>
                </AuthorizeView>
            </div>
        </div>

        <article class="px-4 py-3">
            @Body
        </article>
    </main>
</div>
```

`@code`: `ToggleTheme` invoca `window.mecanoTheme.toggle`; `OnAfterRenderAsync(firstRender)` llama `window.mecanoTheme.syncIcons` para pintar el ícono del toggle (luna/sol) según el tema inicial. `HandleLogout` conserva el flujo JS original (`window.authFetch.logout` a `/api/auth/logout` + `NavigateTo("/login", forceLoad: true)`).

#### `NavMenu.razor`
```razor
<div class="top-row ps-3 navbar navbar-dark">
    <div class="container-fluid">
        <a class="navbar-brand" href="">
            <i class="bi bi-gear-wide-connected" aria-hidden="true"></i>
            <span>MECANO</span>
        </a>
    </div>
</div>

<input type="checkbox" title="Navigation menu" class="navbar-toggler" />

<div class="nav-scrollable" onclick="document.querySelector('.navbar-toggler').click()">
    <nav class="nav flex-column">
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="" Match="NavLinkMatch.All">
                <i class="bi bi-speedometer2" aria-hidden="true"></i> Inicio
            </NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="agendar">
                <i class="bi bi-calendar-plus" aria-hidden="true"></i> Agendar Cita
            </NavLink>
        </div>

        <AuthorizeView Roles="Administrador">
            <div class="section-header">Gestión</div>
            <div class="nav-item px-3">
                <NavLink class="nav-link" href="clientes">
                    <i class="bi bi-people" aria-hidden="true"></i> Clientes y Vehículos
                </NavLink>
            </div>
            <div class="nav-item px-3">
                <NavLink class="nav-link" href="categorias">
                    <i class="bi bi-tags" aria-hidden="true"></i> Categorías y Servicios
                </NavLink>
            </div>
            <div class="nav-item px-3">
                <NavLink class="nav-link" href="mecanicos">
                    <i class="bi bi-tools" aria-hidden="true"></i> Mecánicos
                </NavLink>
            </div>
        </AuthorizeView>

        <div class="nav-item px-3">
            <NavLink class="nav-link" href="connectiontest">
                <i class="bi bi-list-nested-nav-menu" aria-hidden="true"></i> TEST
            </NavLink>
        </div>
    </nav>
</div>
```

### Tema "Industrial Clean" (variables CSS + light/dark)

- **Tokens en `wwwroot/app.css`**: `:root` define la paleta light (slate + acento cian-600 `#0284C7`); `[data-bs-theme="dark"], body.dark-mode` la versión dark (slate oscuro `#0F172A`/`#1E293B`, sidebar `#020617`, acento cian-400 `#38BDF8`, badges en contraste suave). Ambos bloques fijan `color-scheme`.
- **Tema**: `data-bs-theme` se aplica en `<html>` (Bootstrap 5.3.3 lo usa nativamente para tablas/modales/dropdowns/formularios); `body.dark-mode` se añade como gancho de respaldo. Script inline en `<head>` de `App.razor` aplica el tema guardado antes del primer paint (sin flash).
- **`theme.js`** (`window.mecanoTheme`): `toggle()`/`apply()`/`init()`/`syncIcons()`; persiste en `localStorage` bajo `mecano-theme`; `init()` cae a `prefers-color-scheme` si no hay valor guardado. `syncIcons` alterna `bi-moon-stars`/`bi-sun` en los elementos `.theme-toggle-icon`.
- **Badges de estado opt-in**: `.badge-status-success/-warning/-danger` (BG/texto por token) — no sobre-escriben las utilidades `bg-*` de Bootstrap que ya usan Clientes/Detalle.
- **Sidebar del menú**: siempre oscuro en ambos temas (`var(--bg-sidebar)`); link activo con tinte `--nav-active-bg` + barra izq `--primary-accent` vía `box-shadow: inset 3px`; brand `MECANO` con `bi-gear-wide-connected`.
- **`.btn-primary`/**`a` globales** repuntan a los tokens de acento en vez de los colores hardcodeados de la plantilla.
- El **logout** del header no usa una ruta `/logout` (no existe): reutiliza el `HandleLogout` existente (fetch + navegación forzada).

### Páginas `.razor` (lista completa)

| Ruta (`@page`) | Autorización | Propósito |
|---|---|---|
| `/` | `[Authorize]` | Página principal / home |
| `/agendar` | `[Authorize(Roles = "Administrador")]` | Formulario para agendar cita (InteractiveServer) |
| `/categorias` | `[Authorize(Roles = "Administrador")]` | Lista de categorías con CRUD modal |
| `/categorias/{Id:int}` | `[Authorize(Roles = "Administrador")]` | Detalle de categoría con CRUD de servicios embebido |
| `/mecanicos` | `[Authorize(Roles = "Administrador")]` | Lista de mecánicos: modal crear/editar (credenciales + especialidad), desactivar con confirmación, reactivar directo, toggle "Mostrar inactivos" |
| `/clientes` | `[Authorize]` | Lista de clientes con búsqueda y paginación. Lectura para ambos roles; escritura tras `AuthorizeView Roles="Administrador"`. Desactivar muestra warning de citas futuras (fetch al abrir el modal); **Reactivar ahora tiene modal de confirmación** con warning de vehículos en cascada |
| `/clientes/{Id:int}` | `[Authorize]` | Detalle de cliente: datos, vehículos (fila inactiva con badge `Inactivo`) y citas. CRUD de vehículos + **Transferir a otro cliente** (footer del modal de edición, dos vistas buscar↔confirmar en un solo modal con debounce 400 ms, top-10, solo activos). Desactivar con warning de citas futuras; **Reactivar con modal de confirmación** y warning de cascada. Escritura solo tras `AuthorizeView Roles="Administrador"` |
| `/login` | `[AllowAnonymous]` | Login con EditForm + `window.authFetch` (InteractiveServer) |
| `/acceso-denegado` | `[AllowAnonymous]` | Página de acceso denegado |
| `/not-found` | Ninguna | Página de error 404 |
| `/Error` | Ninguna | Página de manejo de errores |
| `/connectiontest` | `[Authorize(Roles = "Administrador")]` | Prueba de conexión a la BD (InteractiveServer) |

> Nota: `Counter.razor` y `Weather.razor` (páginas de plantilla) fueron eliminadas del proyecto.

### Componentes compartidos en `Shared/`
- `Components/Shared/RedirectToLogin.razor` — redirige a `/login` cuando `NotAuthorized` detecta usuario anónimo.
- Los componentes de layout están en `Components/Layout/`.

### Render mode configurado
- **Predominante**: `InteractiveServer` — la mayoría de páginas tienen `@rendermode InteractiveServer` (AgendarCita, Login, ConnectionTest, Categorias, CategoriaDetalle).
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
- `[Authorize]` en `Home.razor`, `Clientes.razor` y `ClienteDetalle.razor`.
- `[Authorize(Roles = "Administrador")]` en `AgendarCita.razor`, `ConnectionTest.razor`, `Categorias.razor` y `CategoriaDetalle.razor` (usando `Roles.Administrador` en AgendarCita y `"Administrador"` literal en las demás).
- `[AllowAnonymous]` en `Login.razor` y `AccesoDenegado.razor`.
- **Autorización granular en las páginas de Clientes**: ambas usan `[Authorize]` simple, así que un `Mecanico` puede consultar clientes. Los controles de escritura (crear, editar, desactivar, reactivar, eliminar, transferir vehículos y sus modales) están envueltos en `<AuthorizeView Roles="Administrador">` dentro del `.razor`.

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
| **Espaciado de nombres** | `Mecano` (raíz), `Mecano.Entidad.Clases`, `Mecano.Entidad.DTOs`, `Mecano.Entidad.DTOs.Categoria`, `Mecano.Entidad.Constantes`, `Mecano.Data`, `Mecano.Logica.Interfaces`, `Mecano.Logica.Servicios`, `Mecano.Components`, `Mecano.Components.Layout`, `Mecano.Components.Pages.Categoria` |
| **Idioma de identificadores** | Mixto: nombres de clase y propiedades en **inglés** (Email, Password, Nombre, Categoria, Service, etc.), pero comentarios y mensajes de usuario en **español** (errores, placeholders, alerts). |
| **Idioma de comentarios** | Principalmente **español** (resumen de clases, comentarios XML), con algunos bloques de código en inglés (`using System.ComponentModel.DataAnnotations`). |
| **Patrón de nombres para archivos `.razor`** | `PascalCase` para nombres de archivo (Home.razor, AgendarCita.razor, CategoriaDetalle.razor) con code-behind `.razor.cs` en los CRUD (Categorias, CategoriaDetalle). Las rutas `@page` usan minúsculas con guiones/segmentos (`/agendar`, `/connectiontest`, `/categorias/{Id:int}`). |
| **Nullable reference types** | `<Nullable>enable</Nullable>` en csproj — habilitado globalmente. Las entidades usan `string?` y `int?` donde es apropiado. |
| **Uso de `ImplicitUsings`** | `<ImplicitUsings>enable</ImplicitUsings>` — las directivas `global using` se generan automáticamente por el SDK. Los using explícitos en `_Imports.razor` y archivos individuales son redundantes pero presentes por costumbre. |

---

## 11. Riesgos y observaciones (solo hechos)

| Riesgo/Observación | Detalle |
|---|---|
| **Archivos muy grandes** | `ClienteDetalle.razor` tiene **659 líneas** y su code-behind **498** (tras el modal de transferencia y el de reactivación embebidos) — sigue siendo el archivo más grande del proyecto. `AgendarCita.razor` (463) es el segundo. **Deuda aceptada y deferida**: el ticket evaluó un split a `VehiculoFormModal.razor` + `TransferirVehiculoModal.razor` y se pospuso por YAGNI/KISS (cada modal supera ~100 líneas pero comparten solo 4 callbacks); reevaluar si se añade un quinto flujo a esa zona. |
| **TODOs explícitos en el código** | Los DTOs `ActualizarServicioDTO.cs` y `ActualizarCategoriaDTO.cs` llevan `// TODO: remove (no-op on value types)` en la propiedad `Id`. |
| **`CategoriaDTO.CantidadServicios` cuenta servicios inactivos** | Suma todos los `Servicio` asociados (sin filtrar `Activo`). La vista de detalle usa `servicios.Count` del listado filtrado para mostrar el total visible. |
| **Colisión de namespace `Categoria`** | `Mecano.Entidad.DTOs.Categoria` (namespace) se cruza con `Mecano.Entidad.Clases.Categoria` (clase). Los DTOs de Servicio se mantienen planos en `Mecano.Entidad.DTOs` para evitar la misma colisión con la clase `Servicio`. |
| **Colisión de namespace `Cliente`** | `Mecano.Entidad.DTOs.Cliente` (namespace) frente a `Mecano.Entidad.Clases.Cliente` (clase). Compila porque los nombres de tipo difieren (`ClienteDTO` vs `Cliente`) y ambos `using` coexisten. |
| **Namespace de las páginas de Clientes es plural** | `Components/Pages/Cliente/` con `namespace ...Pages.Cliente` **rompía la compilación**: dentro de `Mecano.Components.Pages`, la referencia `Cliente` en `AgendarCita.razor:239,240,302` (`private Cliente? selectedCliente`) pasaba a resolverse contra el namespace hermano y lanzaba `CS0118: 'Cliente' es espacio de nombres pero se usa como tipo`. Se renombró la carpeta y el namespace a `Clientes`. Las rutas `/clientes` y `/clientes/{Id:int}` no cambian. La misma trampa aplica a `Categoria`, que ya usa `Pages/Categoria` — hoy no muerde porque ninguna página de `Pages/` declara una variable de tipo `Categoria`. |
| **`AuthorizeView` anidado dentro de `EditForm`** | Ambos componentes usan un parámetro de contenido llamado `context`, así que un `<AuthorizeView><EditForm>` directo produce `RZ9999` por ambigüedad. Todas las páginas nuevas llevan `Context="authCtx"` en el `AuthorizeView`. |
| **`IDisposable` en componentes Blazor** | `Clientes.razor.cs` y `ClienteDetalle.razor.cs` declaran `public partial class ... : ComponentBase, IDisposable` (ambas solo para CTS de debounce); no debe añadirse además `@implements IDisposable` en el `.razor`, o el `Dispose` queda ambiguo (RZ9999). |
| **Cascade delete desde Cliente** | `MySQLDBContext` declara `Cascade` hacia `Cita` y `Vehiculo`. Por eso `ClienteService` expone `DesactivarAsync` (soft) como operación normal de borrado, y `EliminarDefinitivoAsync` (hard) rechaza la operación si hay vehículos o citas asociadas. |
| **Firmas legadas de `IClienteService`** | `BuscarClientesAsync` y `ObtenerPorIdAsync` devuelven la entidad `Cliente`, no un DTO, porque `AgendarCita.razor:239,297,302` las consume. Las lecturas por DTO son `ObtenerTodosAsync` y `ObtenerDetallePorIdAsync`. `RegistrarAsync` es una envoltura de `CrearAsync` y devuelve `int`. |
| **Filtro de búsqueda no trivial** | `ClienteService.AplicarFiltro` usa `EF.Functions.Like` con comodines `%termino%`; así un término con `%` o `_` se interpreta como comodón. `ContarBusquedaAsync` reutiliza el mismo helper para que el conteo coincida con el listado paginado. |
| **`Mecanico.Cedula` conserva guiones** | A diferencia de `Cliente`, la cédula del mecánico se sigue guardando con guiones y sin capa de normalización ni formateo. No es un bug: es una entidad sin el invariante aplicado. Ver Sección 5.1. |
| **`StringLength` del DTO más laxo que el invariante** | `CrearClienteDTO.CedulaIdentidad` y `ActualizarClienteDTO` aceptan hasta 20 caracteres a propósito: una cédula válida con guiones ocupa 10 antes de normalizar. La regla estricta de 9 dígitos vive solo en `ClienteService.NormalizarYValidar`, de modo que el error llegue a la UI por el `catch (InvalidOperationException)` que ambas páginas ya manejan. Si se agrega `[RegularExpression]` al DTO, el error pasaría a `ValidationMessage` y habría que duplicar la lógica de largo en dos lugares. |
| **Código duplicado evidente** | El patrón de `using var context = await _factory.CreateDbContextAsync();` y `VerifyPassword`/`HashPassword` repetitivo en `AuthServices`. Los métodos de servicio siguen patrón similar (crear contexto, consultar, guardar). |
| **`catch` genérico** | `AuthServices.VerifyPassword` retorna `false` ante cualquier excepción (línea ~72-78) — patrón riesgoso pero intencional para no filtrar detalles. |
| **`Vehiculo.Activo` sin inicializador de entidad** | La entidad declara `public bool Activo { get; set; }` (default `false`) y la migración `20261006160916` no fija `defaultValue`. Hoy es correcto porque los tres puntos de creación (`VehiculoService.CrearAsync`, `CitaService`, `DbSeeder`) asignan `Activo = true` explícito; un futuro `new Vehiculo { ... }` que lo olvide quedaría invisible en listas y selects. |
| **Creación rápida de vehículo sin chequeo de duplicado** | `CitaService.AgendarCitaAsync` normaliza la placa pero no la compara contra el índice único: un duplicado ahí lanza `DbUpdateException` cruda en vez de `InvalidOperationException` con mensaje accionable. **Deuda aceptada** (la normalización reduce el problema pero no lo elimina). |
| **Soft delete de vehículo es invisible (sin reactivar directa)** | No hay `ReactivarAsync` ni toggle de "ver inactivos" (YAGNI): el vehículo desaparece de la lista y del select de agendar. **Excepción**: en el detalle de un cliente inactivo se listan los vehículos inactivos (fila con badge). Un vehículo inactivo puede volver a activo por dos caminos laterales: transferirlo (siempre guarda `Activo = true`) o reactivar su cliente (cascada). Su placa sigue ocupada (índice global) mientras exista. |
| **Reactivación en cascada sin flag `DesactivadoPorCascade`** | `ReactivarAsync` activa TODOS los vehículos del cliente, incluidos los que se habían desactivado **individualmente** antes de desactivar el cliente. Es el comportamiento aprobado en el ticket; el coste es que un vehículo desactivado a propósito "revive" al reactivar el cliente. |
| **`SinHistoria` vs guard de eliminación definitiva** | `ClienteDTO.CantidadVehiculos` es condicional (activo → solo vehículos activos; inactivo → todos), pero `EliminarDefinitivoAsync` exige 0 vehículos **en total**. El caso que queda: un cliente **activo** con todos sus vehículos desactivados muestra el botón Eliminar (conteo visible 0) y el servicio lo rechaza con mensaje claro. Para clientes inactivos el conteo visible ya es el total, así que el botón ni siquiera aparece. Dirección segura. |
| **La reactivación ganó modal de confirmación (cambio de UX)** | Antes del ticket, "Reactivar" en ambas páginas ejecutaba directo. Ahora muestra un modal con warning ("Al reactivar este cliente, TODOS sus vehículos (N) se reactivarán también."), coherente con el texto del ticket que pide confirmación explícita. Justificación documentada en el ticket (Nota 1). |
| **`IDisposable` solo por debounce** | `ClienteDetalle.razor.cs` implementa `IDisposable` únicamente para cancelar el CTS del debounce de búsqueda de destino (con comentario explicándolo). No debe añadirse `@implements IDisposable` en el `.razor` (trampa RZ9999 ya documentada). Alternativa descartada: botón "Buscar" en vez de debounce. |
| **Proyección condicional verificada contra BD** | `CantidadVehiculos = c.Activo ? c.Vehiculos.Count(v => v.Activo) : c.Vehiculos.Count()` se ejecutó contra MySQL real (checker temporal con servicios reales, datos de prueba eliminados al final): EF Core 9 la traduce sin errores y los conteos coinciden con las listas visibles. |
| **Parseo del `InputNumber` de precio depende de la cultura del circuito** | El display de precios es es-CR fijo vía `Formatos.Moneda`, pero `InputNumber` parsea con `CultureInfo.CurrentCulture` del circuito (hoy = cultura del SO/servidor; no hay `AddLocalization`/`UseRequestLocalization` en `Program.cs`). En un servidor con cultura `en-US`, escribir `35000,50` fallaría el parseo (`35000.50` sí funcionaría). **Fuera de alcance del ticket de formato de display** (decisión explícita); si algún día se necesita, agregar localización de requests en `Program.cs`. |
| **Colisión de namespace `Mecanico`** | El nuevo namespace `Mecano.Entidad.DTOs.Mecanico` (los 3 DTOs nuevos) se cruza con la clase `Mecano.Entidad.Clases.Mecanico` — misma trampa documentada con `Categoria` y `Cliente` en Sección 11. `HorarioDisponibleDTO.cs` lo resuelve con un alias explícito: `using MecanicoClase = Mecano.Entidad.Clases.Mecanico;`. Toda página/servicio que importe ambos namespaces debe usar el mismo alias para no romper la compilación (`CS0118`/ambigüedad). |
| **CRUD de mecánicos: backend + UI** | Resuelto: `Mecanicos.razor(.cs)` (`/mecanicos`, página completa administrativa con `[Authorize(Roles = "Administrador")]`) consume el CRUD; enlace en `NavMenu`. Quedan como deuda: el guard de citas futuras en `DesactivarAsync` (más abajo) y el smoke test manual de la UI. |
| **`MecanicoService.DesactivarAsync` no verifica citas futuras** | A diferencia del comportamiento esperado para clientes (warning de citas futuras), el desactivado de mecánico solo baja `Activo` (las citas existentes conservan su `MecanicoId`). Deuda conocida del ticket: decidir si agregar warning/conteo como en cliente. |
| **SYSLIB0060 eliminado** | La extracción a `PasswordHasher` (método estático `Rfc2898DeriveBytes.Pbkdf2`) eliminó las 3 advertencias SYSLIB0060 que tenía `AuthServices.cs`; hoy la build queda solo con las 3 preexistentes de `CategoriaService`. |
| **Refactor de layout "Industrial Clean" (CSS variables + tema light/dark)** | `app.css` declara los tokens `:root` (light) y `[data-bs-theme="dark"], body.dark-mode` (dark). `wwwroot/js/theme.js` (`window.mecanoTheme`) aplica/toggle/persiste en `localStorage` (clave `mecano-theme`); script inline en `<head>` de `App.razor` evita el flash al cargar. El toggle vive en el header (`MainLayout`, botón `.theme-toggle-btn` con ícono `bi-moon-stars`/`bi-sun`). `.badge-status-*` son opt-in (no sobre-escriben `bg-*` de Bootstrap). El sidebar es oscuro en ambos temas. Ver Sección 7. |
| **`NavMenu` ahora secciona por rol** | Los enlaces `Clientes y Vehículos`, `Categorías y Servicios` y `Mecánicos` quedaron bajo `<AuthorizeView Roles="Administrador">` con header `Gestión`. Cambio semántico deliberado: antes los veía cualquier usuario autenticado (incluido un `Mecanico`). `Inicio`, `Agendar Cita` y `TEST` quedan fuera de la sección. El `AuthorizeView` no necesita `Context="authCtx"` (la trampa RZ9999 solo aplica anidado dentro de `EditForm`). |
| **Clase `.content` eliminada del canvas** | El canvas pasó de `article.content px-4` (padding-top 1.1rem) a `article.px-4.py-3`; la regla `.content` ya no existe en `app.css`. Ritmo vertical de todas las páginas cambió levemente (aprobado, Q3-a). |
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
| `Mecano/Entidad/Excepciones/AppointmentOverlapException.cs` | Riesgos 11 |
| `Mecano/Components/Pages/Categoria/Categorias.razor(.cs)` | Sección 7 |
| `Mecano/Components/Pages/Categoria/CategoriaDetalle.razor(.cs)` | Sección 7 |
| `Mecano/Entidad/DTOs/ServicioDTO.cs` · `CrearServicioDTO.cs` · `ActualizarServicioDTO.cs` | Sección 2 y 6 |
| `Mecano/Entidad/DTOs/Categoria/` (3 DTOs) | Sección 2 y 6 |
| `Mecano/Entidad/Constantes/TipoIdentificacion.cs` | Sección 6 (Cliente) |
| `Mecano/Entidad/DTOs/Cliente/` (3 DTOs) | Sección 2 y 6 |
| `Mecano/Logica/Servicios/ClienteService.cs` | Sección 6 |
| `Mecano/Data/Migrations/20261005232947_AgregarTipoIdentificacionCliente.cs` | Sección 4 |
| `Mecano/Components/Pages/Clientes/Clientes.razor(.cs)` | Sección 7 |
| `Mecano/Components/Pages/Clientes/ClienteDetalle.razor(.cs)` | Sección 7 |
| `Mecano/Components/Pages/Mecanicos/Mecanicos.razor(.cs)` | Sección 7 |
| `Mecano/Entidad/DTOs/Mecanico/` (3 DTOs) | Sección 2 y 6 |
| `Mecano/Logica/Utilidades/PasswordHasher.cs` | Sección 6 |
| `Mecano/Entidad/DTOs/CitaDTO.cs` | Sección 2 y 6 |
| `Mecano/Entidad/Utilidades/Formatos.cs` | Sección 5.1 |
| `Mecano/Entidad/DTOs/NuevoVehiculoDTO.cs` · `ActualizarVehiculoDTO.cs` | Sección 2 y 6 |
| `Mecano/Logica/Interfaces/IVehiculoService.cs` · `VehiculoService.cs` | Sección 6 |
| `Mecano/Data/Migrations/20261006160916_Vehiculo-Activo-bool.cs` | Sección 5 |
| `Mecano/wwwroot/app.css` | Sección 7 (tema Industrial Clean) |
| `Mecano/wwwroot/js/theme.js` | Sección 7 |
| `Mecano/Components/Layout/MainLayout.razor(.css)` | Sección 7 |
| `Mecano/Components/Layout/NavMenu.razor(.css)` | Sección 7 |

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

9. **CRUD de vehículos en `ClienteDetalle`** — Resuelto: crear/editar con un único modal (mismo formulario, título distinto según modo) y eliminar con modal de confirmación que reemplaza al formulario (sin apilar modales). Solo `Administrador` escribe; `Mecanico` solo lee la lista. Placa normalizada a `ABC123`, mostrada como `ABC-123`; duplicado chequeado contra todas las filas (incluidas inactivas) antes de guardar. `Vehiculo.Activo` sin inicializador: los tres puntos de creación asignan `true` explícito.

10. **¿Existe alguna política de contraseña compleja más allá de los 6 caracteres minimum?** — Sigue abierto: `LoginDTO`/`RegistroAdminDTO` validan formato, pero no se observó validación de fortaleza en `AuthServices.RegisterAdminAsync`.

11. **¿Cómo se persiste el estado `AuthenticatedUser` entre requests?** — Resuelto: ya no se usa el record en memoria para auth; la sesión vive en la cookie y el estado Blazor se reconstruye vía `CustomAuthenticationStateProvider` (claims del `ClaimsPrincipal`).

12. **CRUD de mecánicos** — Resuelto: backend (DTOs en `Entidad/DTOs/Mecanico/`, `IMecanicoService`/`MecanicoService` extendidos, hashing en `Logica/Utilidades/PasswordHasher.cs` delegado desde `AuthServices`) + UI `Components/Pages/Mecanicos/Mecanicos.razor(.cs)` con enlace en `NavMenu`. Pendiente: smoke test manual y decidir el comportamiento de `DesactivarAsync` frente a citas futuras (ver Sección 11).