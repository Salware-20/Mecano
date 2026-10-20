using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

                var mecCarlos = new Mecanico { Nombre = "Carlos Rodríguez", Email = "carlos@mecano.cr", HashPassword = defaultMecPassword, Cedula = "1-1111-1111", Telefono = "70110001", EspecialidadId = catFrenos.CategoriaId, Activo = true };
                var mecMaria = new Mecanico { Nombre = "María López", Email = "maria@mecano.cr", HashPassword = defaultMecPassword, Cedula = "2-2222-2222", Telefono = "70110002", EspecialidadId = catMotor.CategoriaId, Activo = true };
                var mecJose = new Mecanico { Nombre = "José Hernández", Email = "jose@mecano.cr", HashPassword = defaultMecPassword, Cedula = "3-3333-3333", Telefono = "70110003", EspecialidadId = catSuspension.CategoriaId, Activo = true };
                var mecAna = new Mecanico { Nombre = "Ana Mora", Email = "ana@mecano.cr", HashPassword = defaultMecPassword, Cedula = "4-4444-4444", Telefono = "70110004", EspecialidadId = catMantGeneral.CategoriaId, Activo = true };
                var mecPedro = new Mecanico { Nombre = "Pedro Jiménez", Email = "pedro@mecano.cr", HashPassword = defaultMecPassword, Cedula = "5-5555-5555", Telefono = "70110005", EspecialidadId = catMotor.CategoriaId, Activo = false };

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
                    new[] { vehJuan1, vehJuan2, vehLaura, vehRoberto1, vehRoberto2 },
                    new[] { cita1, cita2, cita3 });

                context.Cita.AddRange(cita1, cita2, cita3);
                context.Cita.AddRange(citasDemo);
                await context.SaveChangesAsync();

                _logger.LogInformation("Citas sembradas: 3 base + {Demo} de demostración = {Total} en total.",
                    citasDemo.Count, 3 + citasDemo.Count);
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

        /// <summary>
        /// 50 citas de demostración para que el panel se pruebe con un taller cargado:
        /// ~2 citas por día laborable en las dos semanas vecinas y 10 el día actual,
        /// repartidas en los cinco estados (hoy conviven los cinco a la vez, con la
        /// historia del día: lo de la mañana ya finalizado y lo de la tarde pendiente).
        /// Reglas que respeta: horario 08:00–18:00, sin domingos, sin trasapes de mecánico
        /// ni de vehículo (aunque estén canceladas o finalizadas, para que la agenda se vea
        /// lógica), mecánico activo de la especialidad del servicio y cliente = dueño del
        /// vehículo. Determinista (semilla fija): cada recreación de la BD produce las
        /// mismas citas.
        /// </summary>
        private static List<Cita> CrearCitasDemo(
            IReadOnlyList<Servicio> servicios,
            IReadOnlyList<Mecanico> mecanicosActivos,
            IReadOnlyList<Vehiculo> vehiculos,
            IReadOnlyList<Cita> citasBase)
        {
            var rnd = new Random(20261020); // semilla fija: siembra reproducible
            var hoy = DateTime.Today;

            void Barajar<T>(IList<T> lista)
            {
                for (int i = lista.Count - 1; i > 0; i--)
                {
                    int j = rnd.Next(i + 1);
                    (lista[i], lista[j]) = (lista[j], lista[i]);
                }
            }

            // Ocupación previa: las 3 citas base, para no chocar con ellas.
            var ocupadoMecanico = mecanicosActivos
                .ToDictionary(m => m.MecanicoId, _ => new List<(TimeOnly Ini, TimeOnly Fin)>());
            var ocupadoVehiculo = vehiculos
                .ToDictionary(v => v.VehiculoId, _ => new List<(TimeOnly Ini, TimeOnly Fin)>());

            foreach (var cita in citasBase)
            {
                var tramo = (cita.HoraInicio, cita.HoraFin);
                foreach (var asignacion in cita.CitaMecanicos)
                {
                    if (ocupadoMecanico.ContainsKey(asignacion.MecanicoId))
                        ocupadoMecanico[asignacion.MecanicoId].Add(tramo);
                }
                if (ocupadoVehiculo.ContainsKey(cita.VehiculoId))
                    ocupadoVehiculo[cita.VehiculoId].Add(tramo);
            }

            // Si el arranque cae en domingo (taller cerrado) la historia del día se
            // recorre al lunes: nunca se siembran citas para un día no laborable.
            var fechaHistorica = HorarioAtencion.EsDiaNoLaboral(hoy) ? hoy.AddDays(1) : hoy;

            // Reparto total: 10 (día a día, en orden para que la tarde quede pendiente)
            // + 24 pasadas (histórico mayormente finalizado) + 16 futuras (casi todas por agendar).
            var plan = new List<(DateTime Fecha, EstadoCita Estado)>();

            plan.AddRange(new[]
            {
                EstadoCita.Finalizado, EstadoCita.Finalizado, EstadoCita.Cancelado,
                EstadoCita.EnProceso, EstadoCita.EnProceso,
                EstadoCita.Confirmado, EstadoCita.Confirmado,
                EstadoCita.Pendiente, EstadoCita.Pendiente, EstadoCita.Pendiente
            }.Select(estado => (fechaHistorica, estado)));

            var diasPasados = Enumerable.Range(1, 14).Select(d => hoy.AddDays(-d))
                .Where(f => !HorarioAtencion.EsDiaNoLaboral(f)).ToList();
            var diasFuturos = Enumerable.Range(1, 10).Select(d => hoy.AddDays(d))
                .Where(f => !HorarioAtencion.EsDiaNoLaboral(f)).ToList();

            var pasados = Enumerable.Repeat(EstadoCita.Finalizado, 16)
                .Concat(Enumerable.Repeat(EstadoCita.Cancelado, 5))
                .Concat(Enumerable.Repeat(EstadoCita.Pendiente, 3)) // vencidas: nunca se confirmaron
                .ToList();
            var futuros = Enumerable.Repeat(EstadoCita.Pendiente, 9)
                .Concat(Enumerable.Repeat(EstadoCita.Confirmado, 6))
                .Concat(Enumerable.Repeat(EstadoCita.Cancelado, 1))
                .ToList();

            Barajar(pasados);
            Barajar(futuros);
            for (int k = 0; k < pasados.Count; k++)
                plan.Add((diasPasados[k % diasPasados.Count], pasados[k]));
            for (int k = 0; k < futuros.Count; k++)
                plan.Add((diasFuturos[k % diasFuturos.Count], futuros[k]));

            var citas = new List<Cita>();
            var citasPorFecha = new Dictionary<DateTime, int>();
            int correlativo = 0; // ronda global: reparte el trabajo parejo entre mecánicos y vehículos

            foreach (var (fecha, estado) in plan)
            {
                int ordenDelDia = citasPorFecha.TryGetValue(fecha, out var previas) ? previas : 0;
                citasPorFecha[fecha] = ordenDelDia + 1;

                var mecanico = mecanicosActivos[correlativo % mecanicosActivos.Count];
                var vehiculo = vehiculos[correlativo % vehiculos.Count];
                correlativo++;

                // El servicio sale de la especialidad del mecánico (la UI agrupa así las citas).
                var deLaCategoria = servicios.Where(s => s.CategoriaId == mecanico.EspecialidadId).ToList();
                var servicio = deLaCategoria[rnd.Next(deLaCategoria.Count)];
                var duracion = TimeSpan.FromMinutes(servicio.DuracionMinutos);

                // Hueco escalonado cada 55 min para que el día se vea repartido de 8 a 6;
                // si el escalón no cabe antes del cierre, se usa el último encaje posible.
                var desde = HorarioAtencion.Apertura.AddMinutes((ordenDelDia * 55) % 550);
                if (desde.Add(duracion) > HorarioAtencion.Cierre)
                    desde = HorarioAtencion.Cierre.Add(-duracion);

                var hueco = BuscarHueco(ocupadoMecanico[mecanico.MecanicoId], ocupadoVehiculo[vehiculo.VehiculoId], duracion, desde)
                    ?? BuscarHueco(ocupadoMecanico[mecanico.MecanicoId], ocupadoVehiculo[vehiculo.VehiculoId], duracion, HorarioAtencion.Apertura);

                if (hueco is null)
                {
                    // No debería pasar con 4 mecánicos × 10 h: si ocurre, ese día se queda corto.
                    continue;
                }

                var cita = new Cita
                {
                    ClienteId = vehiculo.ClienteId!.Value, // el cliente es el dueño del vehículo
                    VehiculoId = vehiculo.VehiculoId,
                    ServicioId = servicio.ServicioId,
                    CitaMecanicos = new List<CitaMecanico> { new CitaMecanico { MecanicoId = mecanico.MecanicoId } },
                    Fecha = fecha,
                    HoraInicio = hueco.Value,
                    HoraFin = hueco.Value.Add(duracion),
                    Estado = estado
                };

                citas.Add(cita);
                var tramoNuevo = (cita.HoraInicio, cita.HoraFin);
                ocupadoMecanico[mecanico.MecanicoId].Add(tramoNuevo);
                ocupadoVehiculo[vehiculo.VehiculoId].Add(tramoNuevo);
            }

            return citas;
        }

        /// <summary>
        /// Primer tramo de la duración indicada libre a la vez para el mecánico y el
        /// vehículo, desde <paramref name="desde"/> y sin salir del horario de atención.
        /// </summary>
        private static TimeOnly? BuscarHueco(
            List<(TimeOnly Ini, TimeOnly Fin)> ocupadoMecanico,
            List<(TimeOnly Ini, TimeOnly Fin)> ocupadoVehiculo,
            TimeSpan duracion,
            TimeOnly desde)
        {
            var inicio = desde < HorarioAtencion.Apertura ? HorarioAtencion.Apertura : desde;

            while (inicio.Add(duracion) <= HorarioAtencion.Cierre)
            {
                var fin = inicio.Add(duracion);
                var choque = ocupadoMecanico.Concat(ocupadoVehiculo)
                    .Where(t => inicio < t.Fin && t.Ini < fin)
                    .ToList();

                if (choque.Count == 0)
                    return inicio;

                inicio = choque.Max(t => t.Fin); // salta al final del tramo ocupado
            }

            return null;
        }
    }
}
