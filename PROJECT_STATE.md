# PROJECT STATE: Mecano

## 1. Resumen ejecutivo

| Campo | Valor |
|---|---|
| **Nombre del proyecto** | Mecano |
| **Propósito** | Aplicación de taller mecánico para agendar citas, gestionar clientes, vehículos, servicios y mecánicos. |
| **Stack principal** | .NET 10.0 (SDK Web), Blazor Server (Interactive Server), Entity Framework Core 9.0, MySQL 8.0 (Pomelo EF Provider) |
| **Estado general** | Prototipo / MVP — funcionalidades básicas de gestión de citas operativas, con autenticación por cookies y autorización por roles implementadas. |
| **Páginas Blazor** | 11 páginas (.razor) entre Components/Pages y Components/Pages/{Categoria,Clientes}; además Components/Shared/RedirectToLogin.razor y Layout (MainLayout, NavMenu, ReconnectModal) |
| **Entidades de dominio** | 8 clases principales: Administrador, Mecanico, Cliente, Vehiculo, Servicio, Categoria, Cita, NotificacionLog |
| **Invariante de datos** | `Cliente.CedulaIdentidad` (9 dígitos) y `Cliente.Telefono` (8 dígitos, opcional) se persisten **solo con dígitos**. El formato legible se aplica en la capa de presentación vía `Entidad/Utilidades/Formatos.cs`. Ver Sección 5. |
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
│  │  ├─ NuevoVehiculoDTO.cs
│  │  ├─ ServicioDTO.cs        # Read model de Servicio
│  │  ├─ CrearServicioDTO.cs    # Write model con DataAnnotations
│  │  ├─ ActualizarServicioDTO.cs
│  │  ├─ Cliente/
│  │  │  ├─ ClienteDTO.cs        # Read model; TipoIdentificacion solo lectura
│  │  │  ├─ CrearClienteDTO.cs   # Sin campo de tipo: el servicio asigna Nacional
│  │  │  └─ ActualizarClienteDTO.cs
│  │  └─ Categoria/
│  │     ├─ CategoriaDTO.cs
│  │     ├─ CrearCategoriaDTO.cs
│  │     └─ ActualizarCategoriaDTO.cs
│  ├─ Excepciones/
│  │  ├─ InactiveMechanicException.cs
│  │  └─ AppointmentOverlapException.cs
│  ├─ Utilidades/
│  │  └─ Formatos.cs        # Normalización y formateo de cédula/teléfono
│  └─ Constantes/
│     ├─ Roles.cs            # Strings centralizados: "Administrador", "Mecanico"
│     └─ TipoIdentificacion.cs # Enum: Nacional=1, Dimex=2, Pasaporte=3 (Fase 1 solo Nacional)
├─ Data/
│  ├─ MySQLDBContext.cs     # DbContext con DbSet<>
│  ├─ DbSeeder.cs           # Seed de datos iniciales (categorías, servicios, mecánicos, etc.)
│  └─ Migrations/           # 20261003033206_Inicial + 20261005232947_AgregarTipoIdentificacionCliente
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
│  │  ├─ NavMenu.razor      # Enlaces: Home, Agendar, Clientes, Categorías, TEST
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
│     └─ Clientes/               # Namespace plural: 'Cliente' colisiona con la entidad
│        ├─ Clientes.razor        # @page "/clientes", [Authorize] lectura; escritura vía AuthorizeView
│        ├─ Clientes.razor.cs     # búsqueda con debounce + paginación numerada
│        ├─ ClienteDetalle.razor  # @page "/clientes/{Id:int}", [Authorize]
│        └─ ClienteDetalle.razor.cs
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
1. `Cliente (CedulaIdentidad, TipoIdentificacion)` — Unique index compuesto.
2. `Vehiculo.Placa` — Unique index.

### Estado de migraciones
- **Carpeta `Data/Migrations/`**: Dos migraciones — `20261003033206_Inicial.cs` y `20261005232947_AgregarTipoIdentificacionCliente.cs` (cada una con su `.Designer.cs`, más `MySQLDBContextModelSnapshot.cs`).
- **¿Migraciones aplicadas a la BD?**: El seeder usa `Database.EnsureCreatedAsync()` en lugar de `MigrateAsync()`, por lo que el esquema se crea directamente desde el modelo; las migraciones existen como historial pero no se aplican en runtime. `EnsureCreatedAsync` es no-op sobre una BD ya existente, así que **agregar una columna requiere borrar la BD local** (`DROP DATABASE mecano;`) y dejar que el seeder la reconstruya.
- **`TipoIdentificacion` en Cliente**: Columna `int NOT NULL DEFAULT 1`. EF no puede deducir el default porque el enum arranca en `Nacional = 1` (sin miembro `0`), por lo que el `defaultValue` está fijado a mano en la migración; sin él las filas existentes quedarían en `0`, un valor inválido.
- **Índice único de Cliente**: Compuesto `(CedulaIdentidad, TipoIdentificacion)`. El índice único de `CedulaIdentidad` por sí solo se eliminó para permitir que DIMEX y Pasaporte convivan con un Nacional en la Fase 2.
- **Estrategia de acceso a BD**: `IDbContextFactory<MySQLDBContext>` — los servicios crean contexto bajo demanda con `_factory.CreateDbContextAsync()`. No se inyecta `DbContext` directamente.
- **Normalización y formato de cédula/teléfono**: ver Sección 5.1.

### 5.1 Normalización y formato de identificadores numéricos de Cliente

**El problema.** `Cliente.CedulaIdentidad` estaba indexada como cadena cruda. Un usuario que escribía `101110111` y otro que escribía `1-0111-0111` generaban dos filas para la misma persona, porque `"101110111" != "1-0111-0111"` para el índice único `(CedulaIdentidad, TipoIdentificacion)`. En paralelo, el DTO solo validaba `[StringLength(20)]`, así que entradas como `120adsa91028fmask` se aceptaban y guardaban tal cual.

**El invariante.** `CedulaIdentidad` y `Telefono` se guardan únicamente con dígitos, sin guiones ni espacios. El formato con guiones es exclusivamente de presentación.

**`Entidad/Utilidades/Formatos.cs`** — helper estático, namespace `Mecano.Entidad.Utilidades`:

| Método | Comportamiento |
|---|---|
| `SoloDigitos(raw)` | Descarta todo carácter que no sea ASCII `0-9`. Usa `c is >= '0' and <= '9'` en vez de `char.IsDigit` a propósito: `char.IsDigit` acepta dígitos Unicode (ej. `١٢٣` en arábigo-indic) que no corresponden a una cédula costarricense. Devuelve `string.Empty` ante null. |
| `CedulaNacional(raw)` | `101110111` → `1-0111-0111` (1-4-4). Solo formatea si hay exactamente 9 dígitos; ante cualquier otro largo devuelve el input **sin modificar**, para no imprimir guiones en posiciones que no corresponden. |
| `Telefono(raw)` | `88888888` → `8888-8888` (4-4). Misma regla de largo. |

Constantes `LargoCedulaNacional = 9` y `LargoTelefono = 8`, para que el formateo y la validación no puedan divergir. Los 9 dígitos de una cédula nunca pueden interpretarse como teléfono, así que ambos formateadores no entran en conflicto.

**Orden de operaciones en `ClienteService`.** `CrearAsync` y `ActualizarAsync` ejecutan `NormalizarYValidar` (privado, estático) **antes** de `ExisteIdentificacionAsync`. El orden importa: la comprobación de duplicado y el guardado deben operar sobre la misma forma del valor que realmente se persiste. Antes de este cambio el chequeo de duplicado corría sobre el string crudo mientras se guardaba el `.Trim()`, sin normalizar.

`NormalizarYValidar` lanza `InvalidOperationException` en Spanish al violar el largo. El teléfono es opcional: si queda vacío o es solo basura se guarda `null` y no se valida; si viene con dígitos, deben ser exactamente 8.

**`ExisteIdentificacionAsync`** normaliza su propia entrada con `SoloDigitos` aunque ya reciba un valor normalizado (operación idempotente). El método es público y podría llamarse con un valor crudo, en cuyo caso compararía mal contra lo almacenado. Con la normalización aplicada, `101110111` y `1-0111-0111` colisionan correctamente y el segundo intento falla por cédula duplicada en vez de crear un registro nuevo. `RegistrarAsync` hereta todo el comportamiento por delegation a `CrearAsync`.

**Búsqueda.** `AplicarFiltro` compara el término de búsqueda de dos formas para la cédula: tal como lo escribió el usuario, y normalizado a dígitos (`1-0111` encuentra `101110111`). El nombre y la placa no se normalizan porque no son campos numéricos. Sin esto, una fila mostrada como `1-0111-0111` no sería buscable con ese mismo texto.

**Presentación.** `@using Mecano.Entidad.Utilidades` se agregó a `Components/_Imports.razor`, y el formateo se aplica en el markup de `Clientes.razor` (tabla y confirmación de borrado), `ClienteDetalle.razor` (datos y confirmación) y `AgendarCita.razor` (selector de cliente). Se prefirió llamar al helper desde el `.razor` antes que agregar getters computados a `ClienteDTO`: el DTO se puebla con `Select(Proyeccion)` en EF, y cualquier propiedad calculada quedaría sin valor porque EF solo asigna las propiedades mapeadas.

**Datos existentes.** No se agregó migración de normalización. La decisión fue tirar la base: el seeder ya usa `EnsureCreatedAsync`, que es no-op sobre una base existente, así que la reconstrucción era necesaria de todas formas. `DbSeeder` fue actualizado para insertar los tres clientes con dígitos (`101010101`/`88881111`, `202020202`/`88882222`, `303030303`/`88883333`) y respetar el invariante desde el arranque.

**Fuera de alcance.** `Mecanico.Cedula` sigue almacenando guiones (`"3-3333-3333"`). Es otra entidad, sin capa de normalización ni formateo asociado; quitarle los guiones lo volvería ilegible en crudo. `Mecano.Entidad.Utilidades.Formatos` puede reutilizarse para ese fin si más adelante se define la regla.
- **Patrón de conexión**: MySQL a través de `Pomelo.EntityFrameworkCore.MySql`. `Server=localhost;User=root;Database=mecano` — sin autenticación de usuario con password (root local sin password).

---

## 6. Capa de servicios (lógica de negocio)

### Servicios registrados en DI (todos `Scoped`)

| Interfaz | Implementación | Firme de métodos | DI Life |
|---|---|---|---|
| `IClienteService` | `ClienteService` | **Legado:** `BuscarClientesAsync(termino, pagina, tamanioPagina)` (devuelve `Cliente`, sin filtro de Activo)<br>`ObtenerPorIdAsync(clienteId)` (devuelve `Cliente`)<br>`RegistrarAsync(...)` (envoltura de `CrearAsync`, devuelve `int`)<br>**CRUD:** `BuscarClientesPaginadoAsync(termino, pagina, tamanioPagina, incluirInactivos)`<br>`ObtenerTodosAsync(soloActivos)`<br>`ObtenerDetallePorIdAsync(id)`<br>`CrearAsync(dto)` → `int`<br>`ActualizarAsync(dto)`<br>**Estado:** `DesactivarAsync(id)` (soft delete)<br>`ReactivarAsync(id)`<br>`EliminarDefinitivoAsync(id)` (hard delete, exige 0 vehículos y 0 citas)<br>**Helpers:** `ContarBusquedaAsync(termino, incluirInactivos)`<br>`ExisteIdentificacionAsync(cedula, excluirId)` | Scoped |
| `IVehiculoService` | `VehiculoService` | `ObtenerPorClienteAsync(clienteId)`<br>`RegistrarAsync(clienteId, placa, marca, modelo, anio)` | Scoped |
| `IServicioService` | `ServicioService` | `ObtenerTodosActivosAsync()`<br>`ObtenerPorIdAsync(servicioId)`<br>`CalcularHoraFin(horaInicio, duracionMinutos)`<br>`ObtenerTodosAsync(soloActivos = true)`<br>`ObtenerDetallePorIdAsync(id)`<br>`ObtenerPorCategoriaAsync(categoriaId, soloActivos = true)`<br>`CrearAsync(dto)`<br>`ActualizarAsync(dto)`<br>`EliminarAsync(id)` (soft delete)<br>`ReactivarAsync(id)`<br>`ExisteNombreEnCategoriaAsync(nombre, categoriaId, excluirId)` | Scoped |
| `ICategoriaService` | `CategoriaService` | `ObtenerTodasAsync()`<br>`ObtenerPorIdAsync(id)`<br>`CrearAsync(dto)`<br>`ActualizarAsync(dto)`<br>`EliminarAsync(id)`<br>`ExisteNombreAsync(nombre, excluirId)` | Scoped |
| `IMecanicoService` | `MecanicoService` | `ObtenerPorEspecialidadAsync(categoriaId)`<br>`ObtenerDisponiblesAsync(categoriaId, fecha, horaInicio, horaFin)` | Scoped |
| `ICitaService` | `CitaService` | `AgendarCitaAsync(dto)`<br>`CancelarCitaAsync(citaId)`<br>`ObtenerPorRangoAsync(inicio, fin)`<br>`ObtenerPorClienteAsync(clienteId)` → `List<CitaDTO>` | Scoped |
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
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="categorias">
                <span class="bi bi-list-nested-nav-menu" aria-hidden="true"></span> Categorías
            </NavLink>
        </div>
    </nav>
</div>
```

### Páginas `.razor` (lista completa)

| Ruta (`@page`) | Autorización | Propósito |
|---|---|---|
| `/` | `[Authorize]` | Página principal / home |
| `/agendar` | `[Authorize(Roles = "Administrador")]` | Formulario para agendar cita (InteractiveServer) |
| `/categorias` | `[Authorize(Roles = "Administrador")]` | Lista de categorías con CRUD modal |
| `/categorias/{Id:int}` | `[Authorize(Roles = "Administrador")]` | Detalle de categoría con CRUD de servicios embebido |
| `/clientes` | `[Authorize]` | Lista de clientes con búsqueda y paginación. Lectura para ambos roles; escritura tras `AuthorizeView Roles="Administrador"` |
| `/clientes/{Id:int}` | `[Authorize]` | Detalle de cliente: datos, vehículos y citas. Botones de próxima fase deshabilitados |
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
- **Autorización granular en las páginas de Clientes**: ambas usan `[Authorize]` simple, así que un `Mecanico` puede consultar clientes. Los controles de escritura (crear, editar, desactivar, reactivar, eliminar y sus modales) están envueltos en `<AuthorizeView Roles="Administrador">` dentro del `.razor`.

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
| **Archivos muy grandes** | `AgendarCita.razor` tiene 463 líneas — sigue siendo el archivo más grande. `CategoriaDetalle.razor` (~190 líneas) es el segundo más grande. |
| **TODOs explícitos en el código** | Los DTOs `ActualizarServicioDTO.cs` y `ActualizarCategoriaDTO.cs` llevan `// TODO: remove (no-op on value types)` en la propiedad `Id`. |
| **`CategoriaDTO.CantidadServicios` cuenta servicios inactivos** | Suma todos los `Servicio` asociados (sin filtrar `Activo`). La vista de detalle usa `servicios.Count` del listado filtrado para mostrar el total visible. |
| **Colisión de namespace `Categoria`** | `Mecano.Entidad.DTOs.Categoria` (namespace) se cruza con `Mecano.Entidad.Clases.Categoria` (clase). Los DTOs de Servicio se mantienen planos en `Mecano.Entidad.DTOs` para evitar la misma colisión con la clase `Servicio`. |
| **Colisión de namespace `Cliente`** | `Mecano.Entidad.DTOs.Cliente` (namespace) frente a `Mecano.Entidad.Clases.Cliente` (clase). Compila porque los nombres de tipo difieren (`ClienteDTO` vs `Cliente`) y ambos `using` coexisten. |
| **Namespace de las páginas de Clientes es plural** | `Components/Pages/Cliente/` con `namespace ...Pages.Cliente` **rompía la compilación**: dentro de `Mecano.Components.Pages`, la referencia `Cliente` en `AgendarCita.razor:239,240,302` (`private Cliente? selectedCliente`) pasaba a resolverse contra el namespace hermano y lanzaba `CS0118: 'Cliente' es espacio de nombres pero se usa como tipo`. Se renombró la carpeta y el namespace a `Clientes`. Las rutas `/clientes` y `/clientes/{Id:int}` no cambian. La misma trampa aplica a `Categoria`, que ya usa `Pages/Categoria` — hoy no muerde porque ninguna página de `Pages/` declara una variable de tipo `Categoria`. |
| **`AuthorizeView` anidado dentro de `EditForm`** | Ambos componentes usan un parámetro de contenido llamado `context`, así que un `<AuthorizeView><EditForm>` directo produce `RZ9999` por ambigüedad. Todas las páginas nuevas llevan `Context="authCtx"` en el `AuthorizeView`. |
| **`IDisposable` en componentes Blazor** | El code-behind declara la clase como `public partial class Clientes : ComponentBase, IDisposable`; no debe añadirse además `@implements IDisposable` en el `.razor`, o el `Dispose` queda ambiguo. |
| **Cascade delete desde Cliente** | `MySQLDBContext` declara `Cascade` hacia `Cita` y `Vehiculo`. Por eso `ClienteService` expone `DesactivarAsync` (soft) como operación normal de borrado, y `EliminarDefinitivoAsync` (hard) rechaza la operación si hay vehículos o citas asociadas. |
| **Firmas legadas de `IClienteService`** | `BuscarClientesAsync` y `ObtenerPorIdAsync` devuelven la entidad `Cliente`, no un DTO, porque `AgendarCita.razor:239,297,302` las consume. Las lecturas por DTO son `ObtenerTodosAsync` y `ObtenerDetallePorIdAsync`. `RegistrarAsync` es una envoltura de `CrearAsync` y devuelve `int`. |
| **Filtro de búsqueda no trivial** | `ClienteService.AplicarFiltro` usa `EF.Functions.Like` con comodines `%termino%`; así un término con `%` o `_` se interpreta como comodón. `ContarBusquedaAsync` reutiliza el mismo helper para que el conteo coincida con el listado paginado. |
| **`Mecanico.Cedula` conserva guiones** | A diferencia de `Cliente`, la cédula del mecánico se sigue guardando con guiones y sin capa de normalización ni formateo. No es un bug: es una entidad sin el invariante aplicado. Ver Sección 5.1. |
| **`StringLength` del DTO más laxo que el invariante** | `CrearClienteDTO.CedulaIdentidad` y `ActualizarClienteDTO` aceptan hasta 20 caracteres a propósito: una cédula válida con guiones ocupa 10 antes de normalizar. La regla estricta de 9 dígitos vive solo en `ClienteService.NormalizarYValidar`, de modo que el error llegue a la UI por el `catch (InvalidOperationException)` que ambas páginas ya manejan. Si se agrega `[RegularExpression]` al DTO, el error pasaría a `ValidationMessage` y habría que duplicar la lógica de largo en dos lugares. |
| **Código duplicado evidente** | El patrón de `using var context = await _factory.CreateDbContextAsync();` y `VerifyPassword`/`HashPassword` repetitivo en `AuthServices`. Los métodos de servicio siguen patrón similar (crear contexto, consultar, guardar). |
| **`catch` genérico** | `AuthServices.VerifyPassword` retorna `false` ante cualquier excepción (línea ~72-78) — patrón riesgoso pero intencional para no filtrar detalles. |
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
| `Mecano/Entidad/DTOs/CitaDTO.cs` | Sección 2 y 6 |
| `Mecano/Entidad/Utilidades/Formatos.cs` | Sección 5.1 |

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