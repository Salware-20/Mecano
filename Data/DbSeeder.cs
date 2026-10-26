using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mecano.Entidad.Clases;
using Mecano.Entidad.Constantes;
using Mecano.Entidad.Utilidades;
using Mecano.Logica.Servicios;

namespace Mecano.Data
{
    public class DbSeeder
    {
        private readonly IDbContextFactory<MySQLDBContext> _factory;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DbSeeder> _logger;

        public DbSeeder(
            IDbContextFactory<MySQLDBContext> factory,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<DbSeeder> logger)
        {
            _factory = factory;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Inicializa el esquema de la base de datos aplicando las migraciones pendientes.
        /// Si <c>DatabaseSettings:ResetOnStartup</c> está activo (solo Development),
        /// elimina la base de datos antes de migrar para partir de un estado limpio.
        /// </summary>
        public async Task InitializeAsync()
        {
            await using var context = await _factory.CreateDbContextAsync();
            var reset = _configuration.GetValue<bool>("DatabaseSettings:ResetOnStartup");

            if (reset)
            {
                if (!_environment.IsDevelopment())
                    throw new InvalidOperationException("ResetOnStartup is only permitted in Development environment.");

                _logger.LogWarning("ResetOnStartup=true: Dropping database {Db}...", context.Database.GetDbConnection().Database);
                await context.Database.EnsureDeletedAsync();
            }

            await context.Database.MigrateAsync();
        }

        public async Task SeedAsync()
        {
            await using var context = await _factory.CreateDbContextAsync();

            var seedDemoData = _configuration.GetValue<bool?>("DatabaseSettings:SeedDemoData")
                ?? _environment.IsDevelopment();
            var necesitaSeedDemo = seedDemoData && !await context.Categorias.AnyAsync();

            if (necesitaSeedDemo)
            {
                // El hash es costoso (PBKDF2 con 310k iteraciones): solo se calcula
                // cuando de verdad hay que sembrar los datos de demostración.
                var defaultMecPassword = AuthServices.HashPassword("Mecanico123*");

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

                var mecCarlos = new Mecanico { Nombre = "Carlos Rodríguez", Email = CorreoCorporativo.Componer("carlos"), HashPassword = defaultMecPassword, Cedula = Formatos.NormalizarIdentificacion("1-1111-1111", TipoIdentificacion.Nacional), Telefono = Formatos.SoloDigitos("70110001"), EspecialidadId = catFrenos.CategoriaId, Activo = true };
                var mecMaria = new Mecanico { Nombre = "María López", Email = CorreoCorporativo.Componer("maria"), HashPassword = defaultMecPassword, Cedula = Formatos.NormalizarIdentificacion("2-2222-2222", TipoIdentificacion.Nacional), Telefono = Formatos.SoloDigitos("70110002"), EspecialidadId = catMotor.CategoriaId, Activo = true };
                var mecJose = new Mecanico { Nombre = "José Hernández", Email = CorreoCorporativo.Componer("jose"), HashPassword = defaultMecPassword, Cedula = Formatos.NormalizarIdentificacion("3-3333-3333", TipoIdentificacion.Nacional), Telefono = Formatos.SoloDigitos("70110003"), EspecialidadId = catSuspension.CategoriaId, Activo = true };
                var mecAna = new Mecanico { Nombre = "Ana Mora", Email = CorreoCorporativo.Componer("ana"), HashPassword = defaultMecPassword, Cedula = Formatos.NormalizarIdentificacion("4-4444-4444", TipoIdentificacion.Nacional), Telefono = Formatos.SoloDigitos("70110004"), EspecialidadId = catMantGeneral.CategoriaId, Activo = true };
                var mecPedro = new Mecanico { Nombre = "Pedro Jiménez", Email = CorreoCorporativo.Componer("pedro"), HashPassword = defaultMecPassword, Cedula = Formatos.NormalizarIdentificacion("5-5555-5555", TipoIdentificacion.Nacional), Telefono = Formatos.SoloDigitos("70110005"), EspecialidadId = catMotor.CategoriaId, Activo = false };

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
                var vehJuan1 = new Vehiculo { Placa = "ABC123", Marca = "Toyota", Modelo = "Corolla", Anio = 2020, Color = "Blanco", Kilometraje = 45000, Notas = "Cambio de aceite al día.", ClienteId = cliJuan.ClienteId, Activo = true };
                var vehJuan2 = new Vehiculo { Placa = "DEF456", Marca = "Honda", Modelo = "CRV", Anio = 2019, Color = "Gris", Kilometraje = 78000, ClienteId = cliJuan.ClienteId, Activo = true };
                var vehLaura = new Vehiculo { Placa = "GHI789", Marca = "Hyundai", Modelo = "Tucson", Anio = 2021, Color = "Azul", Kilometraje = 21000, ClienteId = cliLaura.ClienteId, Activo = true };
                var vehRoberto1 = new Vehiculo { Placa = "JKL012", Marca = "Nissan", Modelo = "Sentra", Anio = 2018, Color = "Negro", Kilometraje = 96000, ClienteId = cliRoberto.ClienteId, Activo = true };
                var vehRoberto2 = new Vehiculo { Placa = "MNO345", Marca = "Suzuki", Modelo = "Vitara", Anio = 2022, Color = "Rojo", Kilometraje = 15000, Notas = "Pendiente revisión de frenos.", ClienteId = cliRoberto.ClienteId, Activo = true };

                context.Vehiculo.AddRange(vehJuan1, vehJuan2, vehLaura, vehRoberto1, vehRoberto2);
                await context.SaveChangesAsync();

                var tomorrow = DateTime.Today.AddDays(1);
                var dayAfter = DateTime.Today.AddDays(2);
                var today = DateTime.Today.AddDays(0);
                
                var cita1 = new Cita
                {
                    ClienteId = cliJuan.ClienteId,
                    VehiculoId = vehJuan1.VehiculoId,
                    ServicioId = servPastillas.ServicioId,
                    CitaMecanicos = new List<CitaMecanico> { new CitaMecanico { MecanicoId = mecCarlos.MecanicoId } },
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
                    CitaMecanicos = new List<CitaMecanico> { new CitaMecanico { MecanicoId = mecMaria.MecanicoId } },
                    Fecha = dayAfter,
                    HoraInicio = new TimeOnly(10, 0),
                    HoraFin = new TimeOnly(11, 0),
                    Estado = EstadoCita.Pendiente
                };

                var cita3 = new Cita
                {
                    ClienteId = cliRoberto.ClienteId,
                    VehiculoId = vehRoberto1.VehiculoId,
                    ServicioId = servRevision.ServicioId,
                    CitaMecanicos = new List<CitaMecanico> { new CitaMecanico { MecanicoId = mecAna.MecanicoId } },
                    Fecha = today,
                    HoraInicio = new TimeOnly(12, 0),
                    HoraFin = new TimeOnly(12, 30),
                    Estado = EstadoCita.Finalizado
                };

                // 50 citas de demostración más: el panel se pruebe con un taller cargado y
                // los cinco estados conviviendo. Reglas de agenda, especialidades y horarios
                // están en CrearCitasDemo.
                var citasDemo = CrearCitasDemo(
                    new[] { servPastillas, servRectificado, servDiagMotor, servAfinado, servAlineado, servAmortiguadores, servAceite, servRevision },
                    new[] { mecCarlos, mecMaria, mecJose, mecAna },
                    new[] { vehJuan1, vehJuan2, vehLaura, vehRoberto1, vehRoberto2 });

                context.Cita.AddRange(cita1, cita2, cita3);
                context.Cita.AddRange(citasDemo);
                await context.SaveChangesAsync();

                _logger.LogInformation("Citas sembradas: 3 base + {Demo} de demostración = {Total} en total.",
                    citasDemo.Count, 3 + citasDemo.Count);
            }

            // Seed y actualización del Administrador Global ("adminglobal")
            // Las credenciales de producción deben proveerse mediante user-secrets o variables de entorno
            var globalAdminEmail = _configuration["Seed:GlobalAdmin:Email"] ?? CorreoCorporativo.Componer("adminglobal");
            var globalAdminPassword = _configuration["Seed:GlobalAdmin:Password"] ?? "AdminGlobal123*";

            var adminGlobal = await context.Administradors.FirstOrDefaultAsync(a => a.EsAdminGlobal || a.Email == globalAdminEmail);
            if (adminGlobal is null)
            {
                adminGlobal = new Administrador
                {
                    Cedula = Formatos.NormalizarIdentificacion("109990999", TipoIdentificacion.Nacional),
                    Email = globalAdminEmail,
                    Nombre = "Administrador Global",
                    Telefono = Formatos.SoloDigitos("61321206"),
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
                var hashMecanicosDemo = AuthServices.HashPassword("Mecanico123*");
                foreach (var mec in placeholderMechanics)
                {
                    mec.HashPassword = hashMecanicosDemo;
                }
                await context.SaveChangesAsync();
                _logger.LogInformation("Contraseñas de prueba asignadas a mecánicos con placeholder.");
            }
        }

        /// <summary>
        /// 50 citas de demostración, escritas a mano como lista literal de filas
        /// (ventana, día, slot, estado, mecánico, vehículo, servicio): sin aleatoriedad,
        /// sin barajado y sin búsqueda de huecos — los huecos vienen resueltos por
        /// construcción. Ventanas de días sin domingo: 10 para el día actual (fecha
        /// histórica: hoy, o el lunes si hoy es domingo), 24 pasadas (12 días hacia
        /// atrás) y 16 futuras (8 días hacia adelante). Slots fijos 08:00, 11:00, 13:30 y
        /// 15:30 con servicios de hasta 120 min: dos slots distintos nunca se trasapan y
        /// todo cabe antes de las 18:00; el slot 0 usa siempre el servicio corto de la
        /// especialidad, de modo que la mañana cierra a las 09:00 y deja libre la cita
        /// base de las 09:00. Dentro de un mismo día y slot cada fila lleva mecánico y
        /// vehículo distintos (nunca se trasapan), y las filas de hoy dejan libre la cita
        /// base de las 12:00 (la fila que usa ese mecánico/vehículo a las 11:00 termina a
        /// las 12:00 exactas). Estados repartidos: 18 Finalizado, 15 Pendiente,
        /// 8 Confirmado, 7 Cancelado y 2 EnProceso, más las 3 citas base. Determinista:
        /// cada recreación de la BD produce exactamente las mismas 50 citas.
        /// </summary>
        private static List<Cita> CrearCitasDemo(
            IReadOnlyList<Servicio> servicios,
            IReadOnlyList<Mecanico> mecanicosActivos,
            IReadOnlyList<Vehiculo> vehiculos)
        {
            var hoy = DateTime.Today;
            var fechaHistorica = HorarioAtencion.EsDiaNoLaboral(hoy) ? hoy.AddDays(1) : hoy;

            // Días de las tres ventanas (los domingos el taller cierra).
            var diasPasados = new List<DateTime>();
            for (int i = 1; i <= 14; i++)
            {
                var fecha = hoy.AddDays(-i);
                if (!HorarioAtencion.EsDiaNoLaboral(fecha)) diasPasados.Add(fecha);
            }

            var diasFuturos = new List<DateTime>();
            for (int i = 1; i <= 10; i++)
            {
                var fecha = fechaHistorica.AddDays(i);
                if (!HorarioAtencion.EsDiaNoLaboral(fecha)) diasFuturos.Add(fecha);
            }

            // Slots fijos del día: 0 = 08:00, 1 = 11:00, 2 = 13:30, 3 = 15:30.
            var slots = new[] { new TimeOnly(8, 0), new TimeOnly(11, 0), new TimeOnly(13, 30), new TimeOnly(15, 30) };

            // Las 50 filas: (ventana, día, slot, estado, mecánico, vehículo, servicio).
            // ventana: 0 = día actual (fecha histórica), 1 = pasadas, 2 = futuras;
            // día = índice dentro de esa ventana; mecánico: 0 Carlos, 1 María, 2 José, 3 Ana;
            // servicio según la especialidad del mecánico: Carlos{0,1} María{2,3}
            // José{4,5} Ana{6,7} (los pares cortos: 30–60 min; los impares: 90–120 min).
            (int Ventana, int Dia, int Slot, EstadoCita Estado, int Mec, int Veh, int Serv)[] filas =
            {
                // Hoy (10): la mañana ya cerró y la tarde espera administración.
                (0, 0, 0, EstadoCita.Finalizado, 1, 1, 2),
                (0, 0, 0, EstadoCita.Finalizado, 2, 2, 4),
                (0, 0, 0, EstadoCita.Cancelado, 3, 4, 6),
                (0, 0, 1, EstadoCita.EnProceso, 0, 0, 0),
                (0, 0, 1, EstadoCita.EnProceso, 1, 1, 3),
                (0, 0, 1, EstadoCita.Confirmado, 3, 3, 7),
                (0, 0, 2, EstadoCita.Confirmado, 0, 0, 1),
                (0, 0, 2, EstadoCita.Pendiente, 2, 2, 5),
                (0, 0, 3, EstadoCita.Pendiente, 1, 4, 2),
                (0, 0, 3, EstadoCita.Pendiente, 3, 3, 7),

                // Pasadas (24): dos por día, histórico casi todo finalizado.
                (1, 0, 0, EstadoCita.Finalizado, 0, 0, 0),
                (1, 0, 1, EstadoCita.Finalizado, 1, 2, 3),
                (1, 1, 0, EstadoCita.Finalizado, 1, 1, 2),
                (1, 1, 1, EstadoCita.Finalizado, 2, 3, 5),
                (1, 2, 0, EstadoCita.Finalizado, 2, 2, 4),
                (1, 2, 1, EstadoCita.Cancelado, 3, 4, 7),
                (1, 3, 0, EstadoCita.Cancelado, 3, 3, 6),
                (1, 3, 1, EstadoCita.Finalizado, 0, 1, 1),
                (1, 4, 0, EstadoCita.Finalizado, 0, 4, 0),
                (1, 4, 1, EstadoCita.Pendiente, 1, 3, 3),
                (1, 5, 0, EstadoCita.Finalizado, 1, 0, 2),
                (1, 5, 1, EstadoCita.Finalizado, 2, 4, 5),
                (1, 6, 0, EstadoCita.Pendiente, 2, 1, 4),
                (1, 6, 1, EstadoCita.Cancelado, 3, 0, 7),
                (1, 7, 0, EstadoCita.Finalizado, 3, 2, 6),
                (1, 7, 1, EstadoCita.Cancelado, 0, 2, 1),
                (1, 8, 0, EstadoCita.Cancelado, 0, 3, 0),
                (1, 8, 1, EstadoCita.Finalizado, 1, 0, 3),
                (1, 9, 0, EstadoCita.Finalizado, 1, 4, 2),
                (1, 9, 1, EstadoCita.Finalizado, 2, 1, 5),
                (1, 10, 0, EstadoCita.Finalizado, 2, 0, 4),
                (1, 10, 1, EstadoCita.Pendiente, 3, 2, 7),
                (1, 11, 0, EstadoCita.Finalizado, 3, 1, 6),
                (1, 11, 1, EstadoCita.Finalizado, 0, 4, 1),

                // Futuras (16): dos por día, casi todas por confirmar.
                (2, 0, 0, EstadoCita.Pendiente, 0, 0, 0),
                (2, 0, 1, EstadoCita.Confirmado, 1, 3, 3),
                (2, 1, 0, EstadoCita.Pendiente, 1, 1, 2),
                (2, 1, 1, EstadoCita.Pendiente, 2, 4, 5),
                (2, 2, 0, EstadoCita.Confirmado, 2, 2, 4),
                (2, 2, 1, EstadoCita.Confirmado, 3, 0, 7),
                (2, 3, 0, EstadoCita.Pendiente, 3, 3, 6),
                (2, 3, 1, EstadoCita.Pendiente, 0, 1, 1),
                (2, 4, 0, EstadoCita.Pendiente, 0, 4, 0),
                (2, 4, 1, EstadoCita.Confirmado, 1, 2, 3),
                (2, 5, 0, EstadoCita.Confirmado, 1, 0, 2),
                (2, 5, 1, EstadoCita.Pendiente, 2, 3, 5),
                (2, 6, 0, EstadoCita.Pendiente, 2, 1, 4),
                (2, 6, 1, EstadoCita.Confirmado, 3, 4, 7),
                (2, 7, 0, EstadoCita.Cancelado, 3, 2, 6),
                (2, 7, 1, EstadoCita.Pendiente, 0, 0, 1),
            };

            var citas = new List<Cita>();
            foreach (var (ventana, dia, slot, estado, mec, veh, serv) in filas)
            {
                var fecha = ventana switch
                {
                    0 => fechaHistorica,
                    1 => diasPasados[dia],
                    _ => diasFuturos[dia]
                };

                var mecanico = mecanicosActivos[mec];
                var vehiculo = vehiculos[veh];
                var servicio = servicios[serv];
                var inicio = slots[slot];

                citas.Add(new Cita
                {
                    ClienteId = vehiculo.ClienteId!.Value, // el cliente es el dueño del vehículo
                    VehiculoId = vehiculo.VehiculoId,
                    ServicioId = servicio.ServicioId,
                    CitaMecanicos = new List<CitaMecanico> { new CitaMecanico { MecanicoId = mecanico.MecanicoId } },
                    Fecha = fecha,
                    HoraInicio = inicio,
                    HoraFin = inicio.AddMinutes(servicio.DuracionMinutos),
                    Estado = estado
                });
            }

            return citas;
        }
    }
}
