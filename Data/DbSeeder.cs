using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mecano.Entidad.Clases;

namespace Mecano.Data
{
    public class DbSeeder
    {
        private readonly IDbContextFactory<MySQLDBContext> _factory;
        
        public DbSeeder(IDbContextFactory<MySQLDBContext> factory) => _factory = factory;
        
        public async Task SeedAsync()
        {
            using var context = await _factory.CreateDbContextAsync();
            await context.Database.EnsureCreatedAsync();

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

                var mecCarlos = new Mecanico { Nombre = "Carlos Rodríguez", Email = "carlos@mecano.cr", HashPassword = "PLACEHOLDER_HASH", Cedula = "1-1111-1111", EspecialidadId = catFrenos.CategoriaId, Activo = true };
                var mecMaria = new Mecanico { Nombre = "María López", Email = "maria@mecano.cr", HashPassword = "PLACEHOLDER_HASH", Cedula = "2-2222-2222", EspecialidadId = catMotor.CategoriaId, Activo = true };
                var mecJose = new Mecanico { Nombre = "José Hernández", Email = "jose@mecano.cr", HashPassword = "PLACEHOLDER_HASH", Cedula = "3-3333-3333", EspecialidadId = catSuspension.CategoriaId, Activo = true };
                var mecAna = new Mecanico { Nombre = "Ana Mora", Email = "ana@mecano.cr", HashPassword = "PLACEHOLDER_HASH", Cedula = "4-4444-4444", EspecialidadId = catMantGeneral.CategoriaId, Activo = true };
                var mecPedro = new Mecanico { Nombre = "Pedro Jiménez", Email = "pedro@mecano.cr", HashPassword = "PLACEHOLDER_HASH", Cedula = "5-5555-5555", EspecialidadId = catMotor.CategoriaId, Activo = false };

                context.Mecanico.AddRange(mecCarlos, mecMaria, mecJose, mecAna, mecPedro);
                await context.SaveChangesAsync();

                var cliJuan = new Cliente { NombreCompleto = "Juan Pérez", CedulaIdentidad = "1-0101-0101", Telefono = "8888-1111" };
                var cliLaura = new Cliente { NombreCompleto = "Laura Vargas", CedulaIdentidad = "2-0202-0202", Telefono = "8888-2222" };
                var cliRoberto = new Cliente { NombreCompleto = "Roberto Solís", CedulaIdentidad = "3-0303-0303", Telefono = "8888-3333" };

                context.Cliente.AddRange(cliJuan, cliLaura, cliRoberto);
                await context.SaveChangesAsync();

                var vehJuan1 = new Vehiculo { Placa = "ABC-123", Marca = "Toyota", Modelo = "Corolla", Anio = 2020, ClienteId = cliJuan.ClienteId };
                var vehJuan2 = new Vehiculo { Placa = "DEF-456", Marca = "Honda", Modelo = "CRV", Anio = 2019, ClienteId = cliJuan.ClienteId };
                var vehLaura = new Vehiculo { Placa = "GHI-789", Marca = "Hyundai", Modelo = "Tucson", Anio = 2021, ClienteId = cliLaura.ClienteId };
                var vehRoberto1 = new Vehiculo { Placa = "JKL-012", Marca = "Nissan", Modelo = "Sentra", Anio = 2018, ClienteId = cliRoberto.ClienteId };
                var vehRoberto2 = new Vehiculo { Placa = "MNO-345", Marca = "Suzuki", Modelo = "Vitara", Anio = 2022, ClienteId = cliRoberto.ClienteId };

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
                    Estado = EstadoCita.Pendiente
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
        }
    }
}
