using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mecano.Entidad.Clases;
using Mecano.Entidad.Constantes;
using Mecano.Logica.Servicios;

namespace Mecano.Data
{
    public class DbSeeder
    {
        private readonly IDbContextFactory<MySQLDBContext> _factory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DbSeeder> _logger;

        public DbSeeder(
            IDbContextFactory<MySQLDBContext> factory,
            IConfiguration configuration,
            ILogger<DbSeeder> logger)
        {
            _factory = factory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            using var context = await _factory.CreateDbContextAsync();
            await context.Database.EnsureCreatedAsync();

            var defaultMecPassword = AuthServices.HashPassword("Mecanico123*");

            if (!await context.Categorias.AnyAsync())
            {
                var catFrenos = new Categoria { Nombre = "Frenos", Descripcion = "Servicios relacionados con el sistema de frenos" };
                var catMotor = new Categoria { Nombre = "Motor", Descripcion = "Diagnóstico y reparación del motor" };
                var catSuspension = new Categoria { Nombre = "Suspensión", Descripcion = "Reparación y ajuste de suspensión" };
                var catMantGeneral = new Categoria { Nombre = "Mantenimiento General", Descripcion = "Servicios de mantenimiento preventivo" };

                context.Categorias.AddRange(catFrenos, catMotor, catSuspension, catMantGeneral);
                await context.SaveChangesAsync();

                var servPastillas = new Servicio { Nombre = "Cambio de pastillas de freno", CategoriaId = catFrenos.CategoriaId, DuracionMinutos = 45, Precio = 35000 };
                var servRectificado = new Servicio { Nombre = "Rectificado de discos", CategoriaId = catFrenos.CategoriaId, DuracionMinutos = 90, Precio = 50000 };
                var servDiagMotor = new Servicio { Nombre = "Diagnóstico de motor", CategoriaId = catMotor.CategoriaId, DuracionMinutos = 60, Precio = 25000 };
                var servAfinado = new Servicio { Nombre = "Afinado de motor", CategoriaId = catMotor.CategoriaId, DuracionMinutos = 120, Precio = 75000 };
                var servAlineado = new Servicio { Nombre = "Alineado y balanceo", CategoriaId = catSuspension.CategoriaId, DuracionMinutos = 60, Precio = 30000 };
                var servAmortiguadores = new Servicio { Nombre = "Cambio de amortiguadores", CategoriaId = catSuspension.CategoriaId, DuracionMinutos = 90, Precio = 65000 };
                var servAceite = new Servicio { Nombre = "Cambio de aceite", CategoriaId = catMantGeneral.CategoriaId, DuracionMinutos = 30, Precio = 20000 };
                var servRevision = new Servicio { Nombre = "Revisión general", CategoriaId = catMantGeneral.CategoriaId, DuracionMinutos = 60, Precio = 15000 };

                context.Servicio.AddRange(servPastillas, servRectificado, servDiagMotor, servAfinado, servAlineado, servAmortiguadores, servAceite, servRevision);
                await context.SaveChangesAsync();

                var mecCarlos = new Mecanico { Nombre = "Carlos Rodríguez", Email = "carlos@mecano.cr", HashPassword = defaultMecPassword, Cedula = "1-1111-1111", EspecialidadId = catFrenos.CategoriaId, Activo = true };
                var mecMaria = new Mecanico { Nombre = "María López", Email = "maria@mecano.cr", HashPassword = defaultMecPassword, Cedula = "2-2222-2222", EspecialidadId = catMotor.CategoriaId, Activo = true };
                var mecJose = new Mecanico { Nombre = "José Hernández", Email = "jose@mecano.cr", HashPassword = defaultMecPassword, Cedula = "3-3333-3333", EspecialidadId = catSuspension.CategoriaId, Activo = true };
                var mecAna = new Mecanico { Nombre = "Ana Mora", Email = "ana@mecano.cr", HashPassword = defaultMecPassword, Cedula = "4-4444-4444", EspecialidadId = catMantGeneral.CategoriaId, Activo = true };
                var mecPedro = new Mecanico { Nombre = "Pedro Jiménez", Email = "pedro@mecano.cr", HashPassword = defaultMecPassword, Cedula = "5-5555-5555", EspecialidadId = catMotor.CategoriaId, Activo = false };

                context.Mecanico.AddRange(mecCarlos, mecMaria, mecJose, mecAna, mecPedro);
                await context.SaveChangesAsync();

                // Cédula y teléfono se guardan solo con dígitos (invariante de ClienteService).
                // El formato legible "1-0101-0101" / "8888-1111" lo aplica Formatos en la UI.
                var cliJuan = new Cliente { NombreCompleto = "Juan Pérez", CedulaIdentidad = "101010101", TipoIdentificacion = TipoIdentificacion.Nacional, Telefono = "88881111" };
                var cliLaura = new Cliente { NombreCompleto = "Laura Vargas", CedulaIdentidad = "202020202", TipoIdentificacion = TipoIdentificacion.Nacional, Telefono = "88882222" };
                var cliRoberto = new Cliente { NombreCompleto = "Roberto Solís", CedulaIdentidad = "303030303", TipoIdentificacion = TipoIdentificacion.Nacional, Telefono = "88883333" };

                context.Cliente.AddRange(cliJuan, cliLaura, cliRoberto);
                await context.SaveChangesAsync();

                // La placa se guarda en mayúsculas sin guiones (invariante de VehiculoService).
                // El formato legible "ABC-123" lo aplica Formatos en la UI.
                var vehJuan1 = new Vehiculo { Placa = "ABC123", Marca = "Toyota", Modelo = "Corolla", Anio = 2020, ClienteId = cliJuan.ClienteId, Activo = true };
                var vehJuan2 = new Vehiculo { Placa = "DEF456", Marca = "Honda", Modelo = "CRV", Anio = 2019, ClienteId = cliJuan.ClienteId, Activo = true };
                var vehLaura = new Vehiculo { Placa = "GHI789", Marca = "Hyundai", Modelo = "Tucson", Anio = 2021, ClienteId = cliLaura.ClienteId, Activo = true };
                var vehRoberto1 = new Vehiculo { Placa = "JKL012", Marca = "Nissan", Modelo = "Sentra", Anio = 2018, ClienteId = cliRoberto.ClienteId, Activo = true };
                var vehRoberto2 = new Vehiculo { Placa = "MNO345", Marca = "Suzuki", Modelo = "Vitara", Anio = 2022, ClienteId = cliRoberto.ClienteId, Activo = true };

                context.Vehiculo.AddRange(vehJuan1, vehJuan2, vehLaura, vehRoberto1, vehRoberto2);
                await context.SaveChangesAsync();

                var tomorrow = DateTime.Today.AddDays(1);
                var dayAfter = DateTime.Today.AddDays(2);
                
                var cita1 = new Cita
                {
                    ClienteId = cliJuan.ClienteId,
                    VehiculoId = vehJuan1.VehiculoId,
                    ServicioId = servPastillas.ServicioId,
                    MecanicoId = mecCarlos.MecanicoId,
                    Fecha = tomorrow,
                    HoraInicio = new TimeOnly(9, 0),
                    HoraFin = new TimeOnly(9, 45),
                    Estado = EstadoCita.EnProceso
                };
                
                var cita2 = new Cita
                {
                    ClienteId = cliLaura.ClienteId,
                    VehiculoId = vehLaura.VehiculoId,
                    ServicioId = servDiagMotor.ServicioId,
                    MecanicoId = mecMaria.MecanicoId,
                    Fecha = dayAfter,
                    HoraInicio = new TimeOnly(10, 0),
                    HoraFin = new TimeOnly(11, 0),
                    Estado = EstadoCita.Pendiente
                };

                context.Cita.AddRange(cita1, cita2);
                await context.SaveChangesAsync();
            }

            // Seed y actualización del Administrador Global ("adminglobal")
            // Las credenciales de producción deben proveerse mediante user-secrets o variables de entorno
            var globalAdminEmail = _configuration["Seed:GlobalAdmin:Email"] ?? "adminglobal@mecano.cr";
            var globalAdminPassword = _configuration["Seed:GlobalAdmin:Password"] ?? "AdminGlobal123*";

            var adminGlobal = await context.Administradors.FirstOrDefaultAsync(a => a.EsAdminGlobal || a.Email == globalAdminEmail);
            if (adminGlobal is null)
            {
                adminGlobal = new Administrador
                {
                    Cedula = "109990999",
                    Email = globalAdminEmail,
                    Nombre = "Administrador Global",
                    Telefono = "61321206",
                    Activo = true,
                    EsAdminGlobal = true,
                    HashPassword = AuthServices.HashPassword(globalAdminPassword)
                };
                await context.Administradors.AddAsync(adminGlobal);
                await context.SaveChangesAsync();
                _logger.LogInformation("Global Admin creado exitosamente ({Email}).", globalAdminEmail);
            }
            else
            {
                bool modified = false;
                if (!adminGlobal.EsAdminGlobal)
                {
                    adminGlobal.EsAdminGlobal = true;
                    modified = true;
                }
                if (!adminGlobal.Activo)
                {
                    adminGlobal.Activo = true;
                    modified = true;
                }
                if (string.IsNullOrEmpty(adminGlobal.HashPassword) 
                    || adminGlobal.HashPassword == "PLACEHOLDER_HASH" 
                    || !adminGlobal.HashPassword.Contains('$'))
                {
                    var rawPassword = (!string.IsNullOrEmpty(adminGlobal.HashPassword) && adminGlobal.HashPassword != "PLACEHOLDER_HASH")
                        ? adminGlobal.HashPassword
                        : globalAdminPassword;

                    adminGlobal.HashPassword = AuthServices.HashPassword(rawPassword);
                    modified = true;
                }
                if (adminGlobal.Email == "Admin@mecano.com" && globalAdminEmail != "Admin@mecano.com")
                {
                    adminGlobal.Email = globalAdminEmail;
                    modified = true;
                }

                if (modified)
                {
                    await context.SaveChangesAsync();
                    _logger.LogInformation("Global Admin actualizado con credenciales válidas ({Email}).", adminGlobal.Email);
                }
            }

            // Actualizar contraseñas de mecánicos existentes si tenían PLACEHOLDER_HASH
            var placeholderMechanics = await context.Mecanico
                .Where(m => m.HashPassword == "PLACEHOLDER_HASH" || string.IsNullOrEmpty(m.HashPassword))
                .ToListAsync();

            if (placeholderMechanics.Any())
            {
                foreach (var mec in placeholderMechanics)
                {
                    mec.HashPassword = defaultMecPassword;
                }
                await context.SaveChangesAsync();
                _logger.LogInformation("Contraseñas de prueba asignadas a mecánicos con placeholder.");
            }
        }
    }
}
