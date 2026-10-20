# PROJECT STATE: Mecano

## 1. Resumen ejecutivo

| Campo | Valor |
|---|---|
| **Nombre del proyecto** | Mecano |
| **Propósito** | Aplicación de taller mecánico para agendar citas, gestionar clientes, vehículos, servicios y mecánicos. |
| **Stack principal** | .NET 10.0 (SDK Web), Blazor Server (Interactive Server), Entity Framework Core 9.0, MySQL 8.0 (Pomelo EF Provider) |
| **Estado general** | Prototipo / MVP — gestión operativa de citas, clientes, vehículos, categorías y servicios, con autenticación por cookies y autorización por roles implementadas. |
| **Páginas Blazor** | 13 páginas (.razor) entre Components/Pages y Components/Pages/{Categoria,Clientes,Mecanicos}; además Components/Shared/RedirectToLogin.razor y Components/Shared/VehiculoFormModal.razor(.cs) (formulario de vehículo compartido) y Layout (MainLayout, LoginLayout, NavMenu, ReconnectModal); Login y AccesoDenegado usan LoginLayout (sin menú lateral). Estilo global "Industrial Clean" con variables CSS light/dark (`wwwroot/app.css` + `wwwroot/js/theme.js`). `wwwroot/js/auth.js` alberga además `window.mecanoRedirectFeedback` (oscurece/anima el menú al redirigir a login; lo invocan `MainLayout` y `RedirectToLogin`). |
| **Entidades de dominio** | 8 clases principales: Administrador, Mecanico, Cliente, Vehiculo, Servicio, Categoria, Cita, NotificacionLog. `Cliente`, `Mecanico` y `Administrador` llevan `TipoIdentificacion` (enum `Nacional=1, Dimex=2, Pasaporte=3`; commit `8ec693b`). |
| **Invariante de datos** | La identificación se normaliza **por tipo** vía `Formatos.NormalizarIdentificacion` y se presenta vía `Formatos.FormatearIdentificacion`: `Nacional` = 9 dígitos (`101110111` ↔ `1-0111-0111`); `DIMEX` = 11–12 dígitos, display crudo; `Pasaporte` = alfanumérico ≤20 (solo `Trim`, preserva mayúsculas), display crudo. `Cliente.Telefono`/`Mecanico.Telefono` (8 dígitos; el del mecánico obligatorio desde las regulaciones de entrada, ver Sección 5.1) y `Vehiculo.Placa` (6 caracteres, mayúsculas sin guiones, ej. `ABC123`) mantienen su invariante. El formato legible se aplica en presentación vía `Entidad/Utilidades/Formatos.cs`. Ver Sección 5.1. Los precios (`Servicio.Precio`) se presentan como colones vía `Formatos.Moneda` (formato fijo: punto de miles, coma decimal, decimales dinámicos) y las duraciones (`Servicio.DuracionMinutos`) de forma legible vía `Formatos.Duracion` (`90` → `1 hora 30 minutos`). |
| ** Keep “YAGNI” and “KISS” in mind ** | I don't want a project with a super-complex backend |
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
│  │  ├─ CitaMecanico.cs    # Entidad de unión N:M Cita↔Mecanico (PK compuesta CitaId+MecanicoId); permite varios mecánicos de la misma especialidad por cita
│  │  ├─ Servicio.cs
│  │  ├─ Categoria.cs
│  │  └─ NotificacionLog.cs
│  ├─ DTOs/                  # Objetos de transferencia de datos
│  │  ├─ AuthenticatedUser.cs
│  │  ├─ LoginDTO.cs
│  │  ├─ RegistroAdminDTO.cs  # Incluye Cedula (opcional) + TipoIdentificacion (default Nacional)
│  │  ├─ AgendarCitaDTO.cs      # Write model de agendado: ClienteId, VehiculoId, ServicioId, MecanicosIds (List<int>, [MinLength(1)]), Fecha, HoraInicio (ya NO incluye NuevoVehiculo)
│  │  ├─ HorarioDisponibleDTO.cs
│  │  ├─ CalendarEventDTO.cs
│  │  ├─ CitaDTO.cs            # Proyección de lectura de Cita (cliente, servicio, placa) + `Mecanicos` (List<MecanicoDTO>) y la propiedad calculada `MecanicosNombres` (nombres separados por coma, para las tablas) + ids de cada relación + `ClienteActivo` (advertencia de cliente inactivo en /citas) + `ServicioActivo` (insignia «Servicio inactivo» cuando el servicio fue desactivado)
│  │  ├─ CitaDetalleDTO.cs      # Proyección de lectura completa para /citas/{id} (cliente/vehículo/servicio + notas + `Mecanicos` (List<MecanicoDTO>) + `ClienteActivo` + `ServicioActivo`); la especialidad se deriva de `Mecanicos.First()`
│  │  ├─ FiltroCitasDTO.cs      # Filtros del listado /citas (desde/hasta, mecánico, estado, término cliente/placa)
│  │  ├─ EditarCitaDTO.cs       # Write model de edición no-agenda: Id, VehiculoId, ServicioId, NotasAdmin (fecha/hora/mecánico = Fase 3)
│  │  ├─ ReprogramarCitaDTO.cs  # Write model de reprogramación (Fase 3): Id, MecanicosIds (List<int>, [MinLength(1)]; reemplaza el conjunto de mecánicos), Fecha, HoraInicio
│  │  ├─ NuevoVehiculoDTO.cs    # Incluye ClienteId, Kilometraje, Color y Notas; DataAnnotations (placa laxo a propósito, regla estricta en el servicio)
│  │  ├─ ActualizarVehiculoDTO.cs # Write model con DataAnnotations, plano (misma convención que los DTOs de Servicio); incluye Kilometraje, Color y Notas
│  │  ├─ VehiculoDetalleDTO.cs  # Read model para /vehiculos/{id}: datos del vehículo (incl. Kilometraje/Color/Notas) + ClienteId/ClienteNombre del dueño
│  │  ├─ ServicioDTO.cs        # Read model de Servicio
│  │  ├─ CrearServicioDTO.cs    # Write model con DataAnnotations
│  │  ├─ ActualizarServicioDTO.cs
│  │  ├─ Cliente/
│  │  │  ├─ ClienteDTO.cs        # Read model; incluye TipoIdentificacion
│  │  │  ├─ CrearClienteDTO.cs   # Incluye TipoIdentificacion (default Nacional); nombre/correo con [RegularExpression] (Patrones) y teléfono obligatorio; la validación por tipo de cédula vive en el servicio
│  │  │  └─ ActualizarClienteDTO.cs # Espejo de CrearClienteDTO (mismas reglas) + Id
│  │  ├─ Categoria/
│  │     ├─ CategoriaDTO.cs
│  │     ├─ CrearCategoriaDTO.cs
│  │     └─ ActualizarCategoriaDTO.cs
│  │  ├─ Mecanico/             # Namespace que colisiona con la clase Mecanico (ver Sección 11)
│  │  │  ├─ MecanicoDTO.cs       # Read model; EspecialidadNombre resuelto desde Categoria; incluye TipoIdentificacion
│  │  │  ├─ CrearMecanicoDTO.cs  # Write model (Password + ConfirmarPassword con [Compare]); Cedula opcional + TipoIdentificacion (default Nacional); nombre/correo/teléfono con las reglas de Patrones
│  │  │  └─ ActualizarMecanicoDTO.cs # Espejo de CrearMecanicoDTO; Password opcional (vacío = conservar hash)
│  │  ├─ Perfil/               # "Mi Perfil" del administrador (ver Sección 7)
│  │  │  ├─ PerfilAdminDTO.cs           # Read model: Nombre, Cedula, TipoIdentificacion, Correo (login, no editable) + CorreoNotificaciones
│  │  │  ├─ ActualizarPerfilAdminDTO.cs # Write model: Nombre + TipoIdentificacion + Cedula (opcional; la normaliza el servicio por tipo) + CorreoNotificaciones (opcional); sin [EmailAddress] a propósito (ver Sección 11) y unicidad del correo validada en el servicio
│  │  │  └─ CambiarPasswordDTO.cs       # PasswordActual + NuevaPassword + ConfirmarPassword ([Compare]); complejidad con regex de lookaheads + .+$ (ver Sección 11)
│  │  └─ Dashboard/            # KPIs del panel de control "/" (ver Secciones 6 y 7)
│  │     ├─ ResumenDiarioDTO.cs        # Raíz: Fecha + CitasHoy + TallerPersonal + Ocupacion
│  │     ├─ CitasHoyDTO.cs             # Total + Conteos (1 por EstadoCita, incl. los que quedan en 0) + Citas ordenadas por hora
│  │     ├─ EstadoConteoDTO.cs         # Estado + Conteo
│  │     ├─ CitaResumenDTO.cs          # Hora/rango, Estado, ClienteNombre, VehiculoDescripcion, Placa (sin formatear), ServicioNombre
│  │     ├─ TallerPersonalDTO.cs       # MecanicosActivos, MecanicosConCitasHoy, ClientesActivos, VehiculosActivos
│  │     └─ OcupacionDiariaDTO.cs      # MinutosOcupados, MinutosDisponibles, MecanicosEnAgenda, Cerrado + Porcentaje calculado
│  ├─ Excepciones/
│  │  ├─ InactiveMechanicException.cs
│  │  ├─ InactiveServiceException.cs
│  │  └─ AppointmentOverlapException.cs
│  ├─ Utilidades/
│  │  ├─ Formatos.cs        # Normalización y formateo por tipo de identificación + teléfono/placa/moneda/duración + nombre/badge de estado de cita + ColorEstado (hex espejo de BadgeEstado, para los gráficos)
│  │  ├─ HorarioAtencion.cs # Horario del taller centralizado: Apertura (8:00), Cierre (18:00), MinutosPorDia (600) y EsDiaNoLaboral (domingo). Lo usan las guardas de CitaService y la ocupación del panel de control
│  │  └─ Patrones.cs        # Regex + mensajes de DataAnnotations compartidos (nombre, correo, teléfono) para clientes y mecánicos
│  └─ Constantes/
│     ├─ Roles.cs            # Strings centralizados: "Administrador", "Mecanico"
│     ├─ TipoIdentificacion.cs # Enum: Nacional=1, Dimex=2, Pasaporte=3 (los 3 tipos ya expuestos en UI; el comentario "Fase 1" del archivo quedó obsoleto, ver Sección 11)
│     └─ FiltroActivo.cs     # Enum de filtro de estado para listados admin: Activos=1, Inactivos=2, Todos=3
├─ Data/
│  ├─ MySQLDBContext.cs     # DbContext con DbSet<>
│  ├─ DbSeeder.cs           # Seed de datos iniciales (categorías, servicios, mecánicos, etc.)
│  └─ Migrations/           # 20261003033206_Inicial + 20261005232947_AgregarTipoIdentificacionCliente + 20261006160916_Vehiculo-Activo-bool + 20261011040945_TipoIdentificacion-mecanico-adminitrador + 20261011150853_AgregarTipoIdentificacionMecanico (las dos últimas se pisan; ver Sección 11) + 20261016235018_AgregarKilometrajeVehiculo + 20261018204110_AgregarSoporteMultiMecanicoCita (N:M Cita↔Mecanico con backfill) + 20261019180239_AgregarCorreoNotificacionesAdmin
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
│     ├─ VehiculoService.cs
│     └─ AdministradorService.cs # Perfil del administrador autenticado (ObtenerPerfilAsync / ActualizarPerfilAsync / CambiarPasswordAsync)
├─ wwwroot/
│  ├─ app.css               # Tokens CSS "Industrial Clean" (light/dark, incluye --text-inverse) + estilos globales + fila de gráficos (.grafico-punto/.grafico-detalle) + enlaces ".enlace-cascada" (relleno en cascada con foco/mouse + superficie de cristal, guarda prefers-reduced-motion)
│  └─ js/
│     ├─ auth.js               # window.authFetch.login/logout (fetch con credentials same-origin) + window.mecanoRedirectFeedback (init/setAuthenticated/assignRedirectClass: oscurece el menú al redirigir a login)
│     └─ theme.js              # window.mecanoTheme: toggle light/dark + persistencia en localStorage
├─ Components/
│  ├─ _Imports.razor          # Directivas using compartidas
│  ├─ Routes.razor           # Configuración del enrutador
│  ├─ App.razor              # Raíz HTML con script de framework Blazor
│  ├─ Layout/
│  │  ├─ MainLayout.razor    # Top-header "Panel de Control" + toggle de tema (light/dark) + dropdown de usuario (incluye enlace "Mi Perfil" a `/perfil`) + sidebar + @Body (canvas px-4 py-3). @code: ToggleTheme (refleja el resultado en ThemeState para los gráficos), ToggleUserMenu/CloseUserMenu, HandleLogout; OnAfterRenderAsync(firstRender) sincroniza ThemeState con window.mecanoTheme.isDark, inicializa window.mecanoRedirectFeedback y le pasa el estado de autenticación
│  │  ├─ MainLayout.razor.css # Sidebar con var(--bg-sidebar); header con var(--bg-header) y var(--border-color)
│  │  ├─ LoginLayout.razor   # Layout anónimo sin sidebar: header con marca MECANO + toggle de tema + @Body; lo usan Login.razor y AccesoDenegado.razor
│  │  ├─ LoginLayout.razor.css # Header y contenedor (.page min-height 100vh); duplica #blazor-error-ui
│  │  ├─ NavMenu.razor       # Brand MECANO; Dashboard, Agendar Cita, Citas; header literal "Gestión" (SIN AuthorizeView: visible para todos los roles — ver Sección 11): Clientes y Vehículos, Categorías y Servicios, Mecánicos, Mi Perfil (enlace `bi-person-circle` a `/perfil`, admin-only por atributo)
│  │  ├─ NavMenu.razor.css    # Links con tokens (--text-sidebar, --nav-active-bg, --nav-hover-bg, --primary-accent)
│  │  ├─ ReconnectModal.razor
│  │  └─ ReconnectModal.razor.css # Modal de reconexión de la plantilla (157 líneas, sin cambios)
│  ├─ Pages/
│     ├─ Home.razor          # @page "/", [Authorize(Roles=Administrador)], InteractiveServer. **Panel de control** (KPIs del día): tarjeta "Citas de hoy" (total + desglose por estado en nav-tabs Blazor-puro con badge de conteo y listado hora·cliente·vehículo·servicio), tarjeta "Ocupación del día" (minutos-mecánico ÷ capacidad, barra con role=progressbar y escala baja/óptima/alta, "Taller cerrado" los domingos) y tarjeta "Taller y personal" (mecánicos activos y con citas hoy, clientes y vehículos activos). Debajo: fila de gráficos ApexCharts 6+6 (donut por estado con leyenda visible + barras horizontales de carga por mecánico) con toggle Semana/Mes y @key por tema. Botón de recarga en el card-header (día + período)
│     ├─ Home.razor.cs       # Code-behind: carga DashboardService.ObtenerResumenDiarioAsync(DateTime.Today) y ObtenerResumenPeriodoAsync (semana lun–dom / mes en curso, default Mes), PestanaActiva (arranca en la primera pestaña con citas), FechaLarga con cultura es-CR, helpers de escala/colour de la ocupación, opciones ApexCharts por tema (ColoresDonut = Formatos.ColorEstado; eje y tooltip de la carga formateados en h/min, espejo JS de Formatos.Duracion) y suscripción a ThemeState.Changed (IDisposable)
│     ├─ AgendarCita.razor  # @page "/agendar", [Authorize(Roles=Administrador)], InteractiveServer. Tablero propio de mecánicos (filas) x días lun-sáb (columnas), sin FullCalendar: cada celda muestra las citas del mecánico ese día y al hacer clic abre el modal de agendado (permite agendar a otro mecánico aunque ya haya una cita en la misma hora). Modal "mecánico primero": el mecánico de la fila llega preseleccionado y se permite **elegir varios mecánicos de la misma especialidad** (lista de checkboxes agrupada por especialidad; al elegir uno, los grupos de otras especialidades quedan deshabilitados). El select de servicios solo ofrece los de esa especialidad (Servicio.CategoriaId == Mecanico.EspecialidadId) y **activos** (se excluyen los servicios desactivados); los horarios en que **cualquiera** de los mecánicos elegidos ya está ocupado se deshabilitan usando las citas de la semana cargada. Un clic en el chip de una cita navega a su detalle (`/citas/{id}`) y detiene la propagación para no disparar el modal de agendado de la celda. El tablero cierra con una leyenda de estados (Pendiente/Confirmado/En Proceso/Finalizado/Cancelado) cuyos swatches reutilizan los mismos tokens `--chip-color` de los chips, por lo que los colores no pueden divergir. Estilos en AgendarCita.razor.css. Los **mecánicos inactivos** se muestran también en el tablero, pero solo si tienen citas no canceladas en la semana visible (`ObtenerTodosAsync(FiltroActivo.Todos)` filtrado por `Activo || citasSemana.SelectMany(c => c.CitaMecanicos.Select(cm => cm.MecanicoId)).Contains(Id)`): su fila se pinta atenuada (clase `tablero-mecanico-inactivo`), lleva un badge `Inactivo` junto al nombre y sus celdas **no** son clicables ni muestran el "+ Agendar" (`PuedeAgendar(dia) && m.Activo`; `AbrirModal` también corta si `!mecanico.Activo`), de modo que no se agendan citas nuevas sobre él; la lista de mecánicos del modal solo ofrece activos. Una cita aparece en la fila de **cada** mecánico asignado (`CitasDe` filtra por `CitaMecanicos.Any(cm => cm.MecanicoId == …)`). Los clientes inactivos siguen apareciendo en la búsqueda y se pueden agendar (contrato legado), pero se advierten visualmente: la fila de resultados se pinta gris con badge `Inactivo`, la tarjeta de cliente seleccionado usa tonos warning, el resumen del modal muestra un `alert-warning` y el chip de una cita de cliente inactivo lleva un ícono de advertencia (`bi-exclamation-circle`) + tooltip "Cliente inactivo".
│     ├─ AgendarCita.razor.css # Estilos del tablero (grid mecánicos×días, chips por estado, resumen del modal) con tokens Industrial Clean, `color-mix()` y ajuste para dark mode.
│     ├─ Login.razor         # @page "/login", @layout LoginLayout, [AllowAnonymous], InteractiveServer. El campo de contraseña es un `InputText` con `type="text"` (nunca `password`) y `autocomplete="off"`, ocultado por CSS (`-webkit-text-security: disc`, alternado con `none`), más un botón de ojo dentro de un `input-group` que alterna la visibilidad
│     ├─ Login.razor.cs      # Modelo de login + login vía window.authFetch (cookie) + retorno a `?ReturnUrl` (solo si es ruta local; si no, "/") con forceLoad; expone `mostrarPassword` y `ToggleMostrarPassword` para el botón de ojo del campo de contraseña
│     ├─ Login.razor.css     # Placeholder del formulario: `::deep input::placeholder` + `::deep input.form-control::placeholder` con `color: var(--placeholder-color)` y `opacity: .2 !important`. El `::deep` es necesario porque el CSS aislado de Blazor solo sella el atributo de scope en el HTML del propio `.razor`, y el `<input>` lo renderiza `<InputText>` (ver Sección 11)
│     ├─ AccesoDenegado.razor # @page "/acceso-denegado", @layout LoginLayout, [AllowAnonymous]
│     ├─ NotFound.razor      # @page "/not-found"
│     ├─ Error.razor         # @page "/Error"
│     ├─ ConnectionTest.razor # @page "/connectiontest", [Authorize(Roles=Administrador)], InteractiveServer
│     ├─ Perfil.razor        # @page "/perfil", [Authorize(Roles=Administrador)], InteractiveServer. Avatar circular con las iniciales de Nombre + badge de rol; datos personales en solo lectura (Nombre, TipoIdentificacion, Cedula, Correo de login) y CorreoNotificaciones editable; tarjeta de cambio de contraseña con el mismo patrón del login (nunca type="password": type="text" + autocomplete="off" + -webkit-text-security + botón de ojo)
│     ├─ Perfil.razor.cs     # Resuelve el admin por el claim NameIdentifier; ObtenerPerfilAsync al cargar; GuardarPerfil / GuardarPassword con alertas de éxito/error; Iniciales() (primera + última palabra, máx. 2 letras)
│     ├─ Perfil.razor.css    # Avatar circular (var(--primary-accent)) + ajustes de la tarjeta de perfil
│     ├─ Categoria/
│     │  ├─ Categorias.razor      # @page "/categorias", [Authorize(Roles=Administrador)], InteractiveServer. Vista de datos patrón Clientes: <h1> + alertas + una .card.shadow-sm con búsqueda en el card-header y "Nueva categoría"; tabla Nombre/Descripción/Servicios/Mecánicos/Acciones (badge-status-success y badge-status-neutral para los conteos); acciones en línea Ver/Editar/Eliminar; modales de crear-editar y de confirmación de eliminación. Sin filtro de Estado (CategoriaDTO no tiene Activo). Búsqueda en memoria por nombre o descripción
│     │  ├─ Categorias.razor.cs   # CargarCategorias (ObtenerTodasAsync), CategoriasVisibles (filtro en memoria), AbrirCrear/AbrirEditar/CerrarModal/Guardar, ConfirmarEliminar/EliminarConfirmado, mensajeExito/mensajeError + HayModalAbierto
│     │  ├─ CategoriaDetalle.razor # @page "/categorias/{Id:int}", [Authorize(Roles=Administrador)]. Breadcrumb + <h1> con "Volver"; card con búsqueda + Estado (FiltroActivo) + "Nuevo servicio"; tabla Nombre/Descripción/Duración/Precio/Estado/Acciones; filas inactivas atenuadas (table-secondary); acciones en línea Editar + Desactivar (bi-pause-circle) / Reactivar (bi-play-circle); modales crear-editar, confirmar desactivación y confirmar reactivación
│     │  └─ CategoriaDetalle.razor.cs # ObtenerPorIdAsync + ObtenerPorCategoriaAsync(Id, soloActivos: false) una sola vez (guard idCargado); ServiciosVisibles (filtro estado + búsqueda en memoria); Guardar (ActualizarAsync/CrearAsync); DesactivarConfirmado (EliminarAsync, baja lógica) / ReactivarConfirmado (ReactivarAsync)
│     ├─ Clientes/               # Namespace plural: 'Cliente' colisiona con la entidad
│     │  ├─ Clientes.razor        # @page "/clientes", [Authorize] lectura; escritura vía AuthorizeView. Modal crear/editar con InputSelect de TipoIdentificacion (Nacional/DIMEX/Pasaporte); tabla muestra identificación formateada + tipo (columna solo para Administrador); fila expandible (clic) con panel de vehículos + citas, placa destacada
│     │  ├─ Clientes.razor.cs     # búsqueda con debounce + paginación numerada; panel expandible (acordeón, carga diferida + caché por cliente); round-trip de TipoIdentificacion en AbrirEditar/Guardar
│     │  ├─ Clientes.razor.css    # Estilos del panel expandible (110 líneas): animación de apertura con respeto a `prefers-reduced-motion`
│     │  ├─ ClienteDetalle.razor  # @page "/clientes/{Id:int}", [Authorize], CRUD de vehículos embebido (modal para Administrador). Datos con FormatearIdentificacion + campo de tipo (ambos solo para Administrador); modal editar con InputSelect de tipo
│     │  └─ ClienteDetalle.razor.cs
│     ├─ Mecanicos/              # Namespace plural por la misma razón (colisión con la clase Mecanico)
│     │  ├─ Mecanicos.razor      # @page "/mecanicos", [Authorize(Roles=Administrador)]. Vista de datos patrón Clientes: una .card.shadow-sm con búsqueda en memoria en el card-header (nombre, cédula cruda/formateada, correo o especialidad) + **Estado** (Activos/Inactivos/Todos) + "Nuevo mecánico" (deshabilitado si no cargaron las especialidades) y card-footer con el conteo; tabla Identificación/Nombre/Email/Teléfono/Especialidad/Estado/Acciones con badge-status-success/badge-status-neutral y filas inactivas atenuadas (table-secondary); modales de crear/editar y de confirmación para desactivar y reactivar
│     │  └─ Mecanicos.razor.cs  # carga la lista completa (FiltroActivo.Todos) y las especialidades (CategoriaService); MecanicosVisibles = filtro de estado + búsqueda en memoria; AbrirEditar recarga la fila contra ObtenerDetallePorIdAsync; Guardar (crear con guardas de contraseña; editar con contraseña vacía = conservar hash); ConfirmarDesactivar/ConfirmarReactivar
│     ├─ Citas/                  # Namespace plural (mismo criterio que Clientes/Mecanicos: la carpeta singular colisionaría con la clase Cita)
│     │  ├─ Citas.razor           # @page "/citas", [Authorize(Roles=Administrador)], InteractiveServer. Listado paginado con filtros (desde/hasta, mecánico, estado, cliente/placa); enlaza al detalle
│     │  ├─ Citas.razor.cs        # filtros + paginación numerada; comparte FiltroCitasDTO entre conteo y consulta
│     │  ├─ CitaDetalle.razor     # @page "/citas/{Id:int}", [Authorize(Roles=Administrador)]. Detalle + modal de edición (vehículo/servicio/notas) y modal de reprogramación (fecha/hora/mecánicos), ambos cuando la cita no está cancelada ni finalizada. La tarjeta lista **todos** los mecánicos asignados y marca el badge `Inactivo` en cada uno que no esté activo (`CitaDetalleDTO.Mecanicos`); el modal de reprogramación avisa si hay inactivos y su lista de checkboxes solo ofrece mecánicos activos, agrupados por especialidad con los grupos de otras especialidades deshabilitados (misma regla que `/agendar`)
│     │  └─ CitaDetalle.razor.cs  # carga el detalle; AbrirEditar/GuardarEdicion; AbrirReprogramar/GuardarReprogramacion (listas de mecánicos, ToggleMecanicoReprogramar); vehículos activos del cliente, servicios de la especialidad de los mecánicos asignados (`Mecanicos.FirstOrDefault()?.EspecialidadId`; conserva los actuales si están inactivos) y horarios ocupados por cualquiera de los mecánicos elegidos desde ObtenerPorRangoAsync. `AbrirReprogramar` carga solo `FiltroActivo.Activos` y preselecciona los asignados que siguen activos (los inactivos se excluyen) para forzar la reasignación; el modal agrupa por especialidad (`MecanicosPorEspecialidad`/`GrupoHabilitado`, grupos ajenos deshabilitados)
│     └─ Vehiculos/              # Namespace plural (mismo criterio: la carpeta singular colisionaría con la clase Vehiculo)
│        ├─ VehiculoDetalle.razor    # @page "/vehiculos/{Id:int}", [Authorize] (Admin + Mecánico). Ficha del vehículo (placa, marca, modelo, año, kilometraje, color, notas, dueño, estado) + historial de citas (todas, más recientes primero) con insignia de estado, resumen por estado y enlace a /citas/{id}. Botón Editar (solo Administrador, modal compartido) y botón "Generar Reporte" (impresión del navegador / Guardar como PDF)
│        └─ VehiculoDetalle.razor.cs # carga VehiculoService.ObtenerDetallePorIdAsync + CitaService.ObtenerPorVehiculoAsync; AbrirEdicion/GuardarEdicion (ActualizarAsync); GenerarReporte (JS.InvokeVoidAsync("print"))
│  └─ Shared/
│     ├─ RedirectToLogin.razor # Redirige a /login cuando no hay auth state
│     ├─ ThemeState.cs       # Estado del tema (IsDark + evento Changed) compartido en el circuito; lo actualiza MainLayout (toggle y primer render) y lo consume el panel de control para re-renderizar sus gráficos ApexCharts
│     └─ VehiculoFormModal.razor(.cs) # Modal de crear/editar vehículo compartido por ClienteDetalle y VehiculoDetalle; el dueño aporta estado y acciones (OnGuardar/OnCancelar + ranura FooterIzquierdo p. ej. Eliminar/Transferir)
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
| Mecano | Blazor-ApexCharts | 7.0.0 (gráficos del panel de control; MIT, assets `_content` desde el propio servidor, sin CDN ni clave. El JS del RCL es un **módulo ES** que se importa solo: no requiere etiqueta `<script>` en `App.razor`) |

### Servicios externos
- **Base de datos**: MySQL 8.0.41 ( configurada en `appsettings.json` como `server=localhost;user=root;password=;database=mecano`)
- No hay servicios de email configurados (NotificationService usa logging simulado).
- No hay servicios de almacenamiento en la nube.

---

## 4. Configuración y arranque

### Program.cs (contenido completo)

```csharp
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
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddApexCharts(); // servicio de ApexCharts (gráficos del panel de control)
builder.Services.AddScoped<ThemeState>(); // tema claro/oscuro compartido en el circuito
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
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    rama => rama.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
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

            // Mecanico - Cita (N:M a través de CitaMecanico): una cita puede tener
            // varios mecánicos de la misma especialidad. Al borrar la cita se borran
            // sus asignaciones; el mecánico no se puede borrar si todavía está asignado.
            modelBuilder.Entity<CitaMecanico>()
                .HasKey(cm => new { cm.CitaId, cm.MecanicoId });

            modelBuilder.Entity<CitaMecanico>()
                .HasOne(cm => cm.Cita)
                .WithMany(c => c.CitaMecanicos)
                .HasForeignKey(cm => cm.CitaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CitaMecanico>()
                .HasOne(cm => cm.Mecanico)
                .WithMany(m => m.CitaMecanicos)
                .HasForeignKey(cm => cm.MecanicoId)
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
| Administrador | `DbSet<Administrador>` | Usuario con credenciales (email/hash password); `Cedula`/`TipoIdentificacion` y `CorreoNotificaciones` (correo secundario opcional, editable desde `/perfil`) |
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
- **Carpeta `Data/Migrations/`**: Ocho migraciones — `20261003033206_Inicial.cs`, `20261005232947_AgregarTipoIdentificacionCliente.cs`, `20261006160916_Vehiculo-Activo-bool.cs`, `20261011040945_TipoIdentificacion-mecanico-adminitrador.cs`, `20261011150853_AgregarTipoIdentificacionMecanico.cs`, `20261016235018_AgregarKilometrajeVehiculo.cs` (agrega `Vehiculo.Kilometraje int NULL`), `20261018204110_AgregarSoporteMultiMecanicoCita.cs` (crea la tabla `CitaMecanico`, backfillea desde `Cita.MecanicoId` y luego dropea la columna) y `20261019180239_AgregarCorreoNotificacionesAdmin.cs` (agrega `Administrador.CorreoNotificaciones varchar(200) NULL`); cada una con su `.Designer.cs`, más `MySQLDBContextModelSnapshot.cs`.
- **¿Migraciones aplicadas a la BD?**: El seeder usa `Database.EnsureCreatedAsync()` en lugar de `MigrateAsync()`, por lo que el esquema se crea directamente desde el modelo; las migraciones existen como historial pero no se aplican en runtime. `EnsureCreatedAsync` es no-op sobre una BD ya existente, así que **agregar una columna requiere actualizar el esquema a mano**. El camino documentado era `DROP DATABASE mecano;` y dejar que el seeder reconstruya, pero eso pierde los datos; para `AgregarSoporteMultiMecanicoCita` se optó por aplicar el DDL equivalente con `mysql.exe` (crear `CitaMecanico`, `INSERT … SELECT` de backfill, drop FK/índice/columna) **preservando las filas existentes**, sin crear `__EFMigrationsHistory` (el runtime no lo usa).
- **`TipoIdentificacion` en Cliente**: Columna `int NOT NULL DEFAULT 1`. EF no puede deducir el default porque el enum arranca en `Nacional = 1` (sin miembro `0`), por lo que el `defaultValue` está fijado a mano en la migración; sin él las filas existentes quedarían en `0`, un valor inválido.
- **`TipoIdentificacion` en Mecanico y Administrador** (commit `8ec693b`): dos migraciones encadenadas — `20261011040945` agrega `TipoIdentificacion int NOT NULL DEFAULT 0` a `Mecanico` y `Administradors`, y `20261011150853` vuelve a agregar `TipoIdentificacion int NOT NULL DEFAULT 1` solo a `Mecanico`. Las entidades declaran default `Nacional` en código. Ver Sección 11 (historial duplicado, inofensivo bajo `EnsureCreatedAsync`).
- **Índice único de Cliente**: Compuesto `(CedulaIdentidad, TipoIdentificacion)`. El índice único de `CedulaIdentidad` por sí solo se eliminó para permitir que DIMEX y Pasaporte convivan con un Nacional en la Fase 2.
- **`Vehiculo.Activo`**: Columna `bool` agregada por `20261006160916` **sin `defaultValue`** (el modelo EF no define `HasDefaultValue`). La entidad no lleva inicializador `= true`; cada punto de creación asigna explícito `Activo = true`: `VehiculoService.CrearAsync`, `CitaService.AgendarCitaAsync` y `DbSeeder`. No existe `ReactivarAsync` de vehículo — un vehículo desactivado desaparece de listas y selects, y se restaura por caminos laterales con UI: **transferirlo** (siempre guarda `Activo = true`) o **reactivar su cliente** (cascada). Ver Sección 11.
- **`Vehiculo.Placa`**: única (índice), almacenada normalizada (`ABC123`). El chequeo de duplicado debe mirar **todas** las filas, incluidas las inactivas (el índice único no filtra por `Activo`): si no, la colisión estallaría como `DbUpdateException` cruda. Ver Sección 5.1.
- **`Administrador.CorreoNotificaciones`**: agregado por `20261019180239_AgregarCorreoNotificacionesAdmin` como `varchar(200) NULL` (MySql CharSet `utf8mb4`), columna **opcional** (`NULL`/vacío = sin correo de notificaciones). Como el runtime usa `EnsureCreatedAsync`, además de versionar la migración se aplicó el DDL equivalente a mano preservando las filas: `ALTER TABLE Administradors ADD COLUMN CorreoNotificaciones varchar(200) NULL AFTER Email;` (verificado con `SHOW COLUMNS`). La unicidad entre administradores **no** tiene índice en BD: la valida `AdministradorService` (case-insensitive, `Trim`, excluyendo al propio admin), en espejo de `MecanicoService.ExisteEmailAsync`.
- **Estrategia de acceso a BD**: `IDbContextFactory<MySQLDBContext>` — los servicios crean contexto bajo demanda con `_factory.CreateDbContextAsync()`. No se inyecta `DbContext` directamente.
- **Normalización y formato de cédula/teléfono/placa**: ver Sección 5.1.

### 5.1 Normalización y formato de identificadores (Cliente, Mecanico, Administrador) y de Vehiculo, y formato de moneda

**El problema original.** `Cliente.CedulaIdentidad` estaba indexada como cadena cruda. Un usuario que escribía `101110111` y otro que escribía `1-0111-0111` generaban dos filas para la misma persona, porque `"101110111" != "1-0111-0111"` para el índice único `(CedulaIdentidad, TipoIdentificacion)`. En paralelo, el DTO solo validaba `[StringLength(20)]`, así que entradas como `120adsa91028fmask` se aceptaban y guardaban tal cual.

**El invariante (multi-tipo desde el commit `8ec693b`).** La identificación se guarda únicamente en forma canónica **según su tipo**: `Nacional` y `DIMEX` solo con dígitos (9 y 11–12 respectivamente); `Pasaporte` con `Trim()` (alfanumérico ≤20, preserva mayúsculas). `Telefono` se guarda solo con dígitos (8, opcional). `Vehiculo.Placa` se guarda en mayúsculas ASCII y solo alfanuméricos, sin guiones (`ABC123`). El formato con guiones es exclusivamente de presentación.

**`Entidad/Utilidades/Formatos.cs`** — helper estático, namespace `Mecano.Entidad.Utilidades`:

| Método | Comportamiento |
|---|---|
| `SoloDigitos(raw)` | Descarta todo carácter que no sea ASCII `0-9`. Usa `c is >= '0' and <= '9'` en vez de `char.IsDigit` a propósito: `char.IsDigit` acepta dígitos Unicode (ej. `١٢٣` en arábigo-indic) que no corresponden a una cédula costarricense. Devuelve `string.Empty` ante null. |
| `CedulaNacional(raw)` | `101110111` → `1-0111-0111` (1-4-4). Solo formatea si hay exactamente 9 dígitos; ante cualquier otro largo devuelve el input **sin modificar**, para no imprimir guiones en posiciones que no corresponden. |
| `NormalizarIdentificacion(raw, tipo)` | Forma de almacenamiento por tipo; lanza `InvalidOperationException` en español si no cumple: `Nacional` → `SoloDigitos`, exige 9 (`"La cédula nacional debe tener exactamente 9 dígitos."`); `Dimex` → `SoloDigitos`, exige 11–12 (`"El DIMEX debe tener entre 11 y 12 dígitos."`); `Pasaporte` → `Trim()`, no vacío y ≤20; `null`/blanco → `"El número de identificación es obligatorio."`. |
| `FormatearIdentificacion(raw, tipo)` | Forma de presentación por tipo: `Nacional` → `CedulaNacional(raw)`; `Dimex`/`Pasaporte` → `raw.Trim()` crudo, sin guiones. |
| `Telefono(raw)` | `88888888` → `8888-8888` (4-4). Misma regla de largo. |
| `NormalizarPlaca(raw)` | `"abc-123"` → `"ABC123"`: mayúsculas ASCII, descarta todo lo que no sea `0-9`/`A-Z`/`a-z`. Es la forma de almacenamiento de `Vehiculo.Placa`; pasa antes del chequeo de duplicado y del guardado (invariante de `VehiculoService`). En `CitaService.AplicarFiltro` se usa para buscar placas escritas con o sin guiones (ver Búsqueda). |
| `Placa(raw)` | `"ABC123"` → `"ABC-123"` (3-3). Solo formatea si el normalizado tiene exactamente 6 caracteres; ante otro largo devuelve el input sin modificar. Misma regla que `CedulaNacional` ante entradas inesperadas. |
| `Moneda(monto)` | `35000` → `₡35.000` y `35000.5` → `₡35.000,50`. Formato de colón con **`NumberFormatInfo` fijo** (campo `static readonly NumberFormatInfo FormatoMonedaCR`, construido una sola vez en `CrearFormatoMonedaCR()` clonando `es-CR`; no depende de la cultura del circuito/servidor): símbolo `₡` U+20A1, **punto** como separador de miles y **coma** decimal (a diferencia del `es-CR` de .NET, que usa espacio no-cortable U+00A0). **Decimales dinámicos**: `C0` si el monto es entero (`monto % 1 == 0`, ej. `₡35.000`) y `C2` si tiene fracción (`₡35.000,50`). Solo presentación — nunca para parsear ni persistir. |
| `Duracion(minutos)` | Convierte minutos a texto legible en español con singular/plural correctos y **omitiendo la parte cero**: `45` → `45 minutos`, `60` → `1 hora`, `90` → `1 hora 30 minutos`, `120` → `2 horas`, `1` → `1 minuto`, `≤ 0` → `0 minutos`. Solo presentación: el dato se persiste crudo como `Servicio.DuracionMinutos` (`int`), y el `ErrorMessage`/invariante de rango (`5–480`) sigue en `CrearServicioDTO`. |

Constantes `LargoCedulaNacional = 9`, `LargoDimexMin = 11`, `LargoDimexMax = 12`, `LargoTelefono = 8` y `LargoPlaca = 6`, para que el formateo y la validación no puedan divergir. Los 9 dígitos de una cédula nunca pueden interpretarse como teléfono, y una placa alfanumérica nunca colisiona con ninguno de los dos formateadores, así que las tres reglas no entran en conflicto.

**Orden de operaciones en `ClienteService`.** `CrearAsync` y `ActualizarAsync` ejecutan `NormalizarYValidar(cedula, telefono, tipo)` (privado, estático; delega en `NormalizarIdentificacion`) **antes** del chequeo de duplicado. El orden importa: la comprobación de duplicado y el guardado deben operar sobre la misma forma del valor que realmente se persiste.

`NormalizarYValidar` lanza `InvalidOperationException` en Spanish al violar el largo. El teléfono es opcional: si queda vacío o es solo basura se guarda `null` y no se valida; si viene con dígitos, deben ser exactamente 8. `VehiculoService` tiene su propio `NormalizarYValidar` privado (placa normalizada de exactamente `LargoPlaca` y `Anio` en 1900–2100) que corre antes del chequeo de duplicado y del guardado; el chequeo debe ver **todas** las filas —incluidas las desactivadas— porque el índice único de `Vehiculo.Placa` es global. El agendado de citas ya **no** crea vehículos al vuelo: `AgendarCitaAsync` exige un `VehiculoId` existente (`InvalidOperationException("Seleccione un vehículo para la cita.")` si es null), por lo que la creación de vehículos pasa siempre por `VehiculoService` (con su chequeo de duplicado).

**Chequeo de duplicado con tipos.** `CrearAsync`/`ActualizarAsync` buscan por cadena normalizada (`c.CedulaIdentidad == cedula`, sin filtrar tipo) y luego ramifican el mensaje: mismo tipo → `"Ya existe un cliente con la identificación {formateada} ({tipo})."`; distinto tipo → `"La identificación {formateada} ya está registrada para otro cliente con el tipo '{tipo}'.“` (donde `formateada = FormatearIdentificacion(cedula, tipoDelDuplicado)`). Nótese que el índice DB es compuesto `(Cedula, Tipo)`, así que un mismo número con distinto tipo pasaría la BD pero el servicio lo rechaza igual (decisión: número globalmente único).

**Identificación y placa: DTO laxo, servicio estricto.** `NuevoVehiculoDTO`/`ActualizarVehiculoDTO` aceptan placas de hasta 20 caracteres a propósito (`"ABC-123"` ocupa 7); la regla estricta de 6 vive solo en `VehiculoService.NormalizarYValidar`, de modo que el error llegue a la UI por el `catch (InvalidOperationException)` que la página ya maneja. Igual para identificación: los 4 DTOs de cliente/mecánico aceptan hasta 20 caracteres y traen `TipoIdentificacion = Nacional` por defecto; la regla estricta por tipo vive solo en `NormalizarIdentificacion`. **No** se les puso `[RegularExpression]`: habría que duplicar en el DTO la lógica de 3 tipos que ya vive en el servicio.

**Regulaciones de entrada de nombre, correo y teléfono (`Entidad/Utilidades/Patrones.cs`).** Para que crear y editar no diverjan, las reglas de nombre y correo se declaran como constantes en `Patrones` y se referencian desde los 4 DTOs de escritura: `Patrones.Nombre` = `^[\p{L} '\-]+$` (solo letras —cualquier alfabeto, incluidas tildes y ñ—, espacios, apóstrofes y guiones) y `Patrones.Correo` = `^[^@\s]+@[^@\s]+\.[a-zA-Z]{2,}$` (exige punto + extensión alfabética ≥2; más estricto que `EmailAddressAttribute`, que acepta dominios sin punto). Cada regla lleva su `ErrorMessage` en español centralizado en `Patrones` (`MensajeNombre`, `MensajeCorreo`, `MensajeTelefonoFormato`, `MensajeTelefonoRequerido`). `[RegularExpression]` valida null/vacío como correcto, así que no duplica el error de `[Required]`. La aplicación es **solo por `DataAnnotations`** en el `EditForm` de Blazor: no hay `Validator.TryValidateObject` ni barrera server-side adicional (los servicios siguen normalizando/validando por su cuenta). El **teléfono es obligatorio y con el invariante de 8 dígitos también en `MecanicoService`** (`NormalizarTelefono`, espejo de `ClienteService.NormalizarYValidar`), y los 5 mecánicos del `DbSeeder` llevan teléfono de 8 dígitos para cumplirlo desde el arranque.

**`ExisteIdentificacionAsync(cedula, excluirId, tipo?)`** — con `tipo` normaliza per-tipo y comprueba `(cedula == normalizada && tipo == tipo)`; sin `tipo` (legado) compara contra `Trim()` crudo o `SoloDigitos`.

**`MecanicoService` normaliza igual.** `CrearAsync`/`ActualizarAsync` aplican `Formatos.NormalizarIdentificacion(dto.Cedula, dto.TipoIdentificacion)`; la cédula del mecánico es **opcional** (blanco → `null`) y el tipo se persiste en `Mecanico.TipoIdentificacion` (default `Nacional` en entidad y DTO). `MecanicoDTO` expone `Cedula` + `TipoIdentificacion`. El teléfono del mecánico es **obligatorio** y se normaliza a 8 dígitos con `NormalizarTelefono` (mismo invariante que `Cliente.Telefono`). `AuthServices.RegisterAdminAsync` ganó overload `(email, nombre, password, cedula?, tipo)` que normaliza igual y persiste `Cedula` + `TipoIdentificacion` en `Administrador`; el overload viejo delega con `(null, Nacional)`.

**Búsqueda.** `AplicarFiltro` compara el término de búsqueda de dos formas para la cédula: tal como lo escribió el usuario, y normalizado a dígitos (`1-0111` encuentra `101110111`). La placa funciona igual: se compara tal cual y normalizada (`ABC-123`, `abc123` y `ABC123` encuentran la fila `ABC123`). El nombre no se normaliza porque no es un campo alfanumérico canónico. Sin esto, una fila mostrada como `1-0111-0111` / `ABC-123` no sería buscable con ese mismo texto. No hay faceta de filtro por tipo: un pasaporte alfanumérico solo matchea por término crudo.

**Presentación.** `@using Mecano.Entidad.Utilidades` se agregó a `Components/_Imports.razor`, y el formateo por tipo (`FormatearIdentificacion(cedula, tipo)` + texto del enum) se aplica en `Clientes.razor` (tabla), `ClienteDetalle.razor` (tarjeta de datos) y `Mecanicos.razor` (tabla). Los modales crear/editar de las tres páginas llevan `InputSelect` con las 3 opciones (Nacional/DIMEX/Pasaporte). Se prefirió llamar al helper desde el `.razor` antes que agregar getters computados a `ClienteDTO`: el DTO se puebla con `Select(Proyeccion)` en EF, y cualquier propiedad calculada quedaría sin valor porque EF solo asigna las propiedades mapeadas. **Deuda**: `AgendarCita.razor:67,83` y los modales de eliminar de `Clientes` / eliminar-transferir de `ClienteDetalle` siguen usando `CedulaNacional` hardcodeado (ignoran el tipo). Para DIMEX el fallback devuelve los dígitos crudos (largo ≠ 9, sin guiones erróneos); un pasaporte cuyos dígitos extraídos sumen exactamente 9 se mostraría con guiones `1-XXXX-XXXX` en esos sitios (ver Sección 11).

**Presentación de precios (colones).** `Formatos.Moneda` es el único punto de formato de moneda de la app (decisión: moneda fija CRC, sin configuración en `Program.cs` ni middleware de localización). El formato usa un `NumberFormatInfo` fijo con el **punto** como separador de miles y la **coma** como decimal, y **decimales dinámicos**: los montos enteros se muestran sin decimales (`₡35.000`) y los que tienen fracción con dos (`₡35.000,50`); el símbolo se mantiene `₡` U+20A1. Se aplica en los sitios donde se muestra `Servicio.Precio`: la columna Precio de la tabla de servicios de `CategoriaDetalle.razor`, el hint en vivo bajo el `InputNumber` de precio del modal de servicio ("Se mostrará como ₡35.000") y las opciones del select de servicio de `AgendarCita.razor` y del detalle/select de `CitaDetalle.razor`. Antes mostraban `$35000` crudo con `$` hardcodeado. Verificado en runtime: `Formatos.Moneda(35000)` → `₡35.000`, `Formatos.Moneda(1000.50m)` → `₡1.000,50` y `Formatos.Moneda(75500.25m)` → `₡75.500,25`, incluso con `CurrentCulture = en-US`.

**Presentación de duraciones.** `Formatos.Duracion` (`90` → `1 hora 30 minutos`) es el único punto de formato legible de duración. Se usa en la **columna "Duración"** de la tabla de servicios de `CategoriaDetalle.razor` (`45 minutos`, `1 hora 30 minutos`, reemplazando el crudo `45 min`; en espejo de la columna de precio con `Formatos.Moneda`) y en el **hint en vivo** del modal de crear/editar servicio ("Entre 5 y 480 minutos. Equivale a **1 hora 30 minutos**."), bajo el `InputNumber` de duración. La duración se persiste cruda (`Servicio.DuracionMinutos`) y solo se embellece en presentación. Verificado en runtime que el hint se actualiza **mientras se escribe** (no solo al salir del campo): `9` → `9 minutos`, `90` → `1 hora 30 minutos`, `75` → `1 hora 15 minutos`, `150` → `2 horas 30 minutos`, `480` → `8 horas`.

**Datos existentes.** No se agregó migración de normalización. La decisión fue tirar la base: el seeder ya usa `EnsureCreatedAsync`, que es no-op sobre una base existente, así que la reconstrucción era necesaria de todas formas. `DbSeeder` siembra tres clientes, todos `TipoIdentificacion.Nacional` con dígitos (`101010101`/`88881111`, `202020202`/`88882222`, `303030303`/`88883333`) — sin seeds DIMEX/Pasaporte — y los vehículos con placas normalizadas y `Activo = true` (`ABC123`, `DEF456`, `GHI789`, `JKL012`, `MNO345`), respetando el invariante desde el arranque.

**Alcance del invariante por entidad.** `Cliente.CedulaIdentidad` (requerida) y `Mecanico.Cedula`/`Administrador.Cedula` (opcionales, `null` si blanco) comparten `NormalizarIdentificacion`/`FormatearIdentificacion`. Solo `Cliente` tiene índice único compuesto `(Cedula, Tipo)`; `Mecanico`/`Administrador` no tienen índice de identificación (la unicidad real entre usuarios es por email vía `ExisteEmailAsync`).
- **Patrón de conexión**: MySQL a través de `Pomelo.EntityFrameworkCore.MySql`. `Server=localhost;User=root;Database=mecano` — sin autenticación de usuario con password (root local sin password).

---

## 6. Capa de servicios (lógica de negocio)

### Servicios registrados en DI (todos `Scoped`)

| Interfaz | Implementación | Firme de métodos | DI Life |
|---|---|---|---|
| `IClienteService` | `ClienteService` | **Legado:** `BuscarClientesAsync(termino, pagina, tamanioPagina)` (devuelve `Cliente`, sin filtro de Activo)<br>`ObtenerPorIdAsync(clienteId)` (devuelve `Cliente`)<br>**CRUD:** `BuscarClientesPaginadoAsync(termino, pagina, tamanioPagina, FiltroActivo filtro, bool incluirIdentificacion)`<br>`ObtenerTodosAsync(soloActivos)`<br>`ObtenerDetallePorIdAsync(id)`<br>`CrearAsync(dto)` → `int` (normaliza por `dto.TipoIdentificacion`; duplicado con mensajes mismo-tipo vs cross-tipo)<br>`ActualizarAsync(dto)` (igual, excluye self)<br>**Estado:** `DesactivarAsync(id)` (soft delete **en cascada**: los vehículos activos del cliente pasan a inactivos; una sola `SaveChangesAsync`)<br>`ReactivarAsync(id)` (**en cascada**: TODOS los vehículos del cliente vuelven a activos, sin flag `DesactivadoPorCascade`)<br>`EliminarDefinitivoAsync(id)` (hard delete, exige 0 vehículos y 0 citas)<br>**Helpers:** `ContarBusquedaAsync(termino, FiltroActivo filtro, bool incluirIdentificacion)`<br>`ExisteIdentificacionAsync(cedula, excluirId, tipo?)` (con tipo: normaliza per-tipo y compara `(cedula, tipo)`; sin tipo: match legado crudo/dígitos) | Scoped |
| `IVehiculoService` | `VehiculoService` | `ObtenerPorClienteAsync(clienteId, incluirInactivos = false)` — solo activos por defecto (el parámetro opcional protege al llamador legado de `AgendarCita`)<br>`ObtenerDetallePorIdAsync(id)` → `VehiculoDetalleDTO?` (proyección con el dueño; `null` si no existe)<br>`CrearAsync(dto)` → `int`<br>`ActualizarAsync(dto)` → `bool` (persiste también `Kilometraje`/`Color`/`Notas`)<br>`EliminarAsync(id)` → `bool` (soft delete: `Activo = false`; sin `ReactivarAsync`)<br>`TransferirAsync(vehiculoId, nuevoClienteId)` → `bool` — reasigna dueño, **reactiva** el vehículo y bloquea si tiene citas futuras | Scoped |
| `IServicioService` | `ServicioService` | `ObtenerTodosActivosAsync()`<br>`ObtenerPorIdAsync(servicioId)`<br>`CalcularHoraFin(horaInicio, duracionMinutos)`<br>`ObtenerTodosAsync(soloActivos = true)`<br>`ObtenerDetallePorIdAsync(id)`<br>`ObtenerPorCategoriaAsync(categoriaId, soloActivos = true)`<br>`CrearAsync(dto)`<br>`ActualizarAsync(dto)`<br>`EliminarAsync(id)` (soft delete)<br>`ReactivarAsync(id)`<br>`ExisteNombreEnCategoriaAsync(nombre, categoriaId, excluirId)` | Scoped |
| `ICategoriaService` | `CategoriaService` | `ObtenerTodasAsync()`<br>`ObtenerPorIdAsync(id)`<br>`CrearAsync(dto)`<br>`ActualizarAsync(dto)`<br>`EliminarAsync(id)`<br>`ExisteNombreAsync(nombre, excluirId)` | Scoped |
| `IMecanicoService` | `MecanicoService` | **Sin uso (heredado de la etapa FullCalendar; candidatos a eliminar):** `ObtenerPorEspecialidadAsync(categoriaId)`<br>`ObtenerDisponiblesAsync(categoriaId, fecha, horaInicio, horaFin)`<br>**Consumido por `AgendarCita` (filas del tablero) y CRUD admin (`Components/Pages/Mecanicos/Mecanicos.razor`):** `ObtenerTodosAsync(FiltroActivo filtro = FiltroActivo.Activos)`<br>`ObtenerDetallePorIdAsync(id)`<br>`CrearAsync(dto)` → `int`<br>`ActualizarAsync(dto)` → `bool`<br>`DesactivarAsync(id)`<br>`ReactivarAsync(id)`<br>`ExisteEmailAsync(email, excluirId)` | Scoped |
| `ICitaService` | `CitaService` | `AgendarCitaAsync(dto)` — guards de agenda (defensa en profundidad, la UI ya los restringe): **todos** los mecánicos de `MecanicosIds` existen y están activos (`InactiveMechanicException`), **comparten especialidad** (`InvalidOperationException`), servicio existe y está activo (`InactiveServiceException`), **vehículo obligatorio** (`InvalidOperationException` si `VehiculoId` es null), fecha ≥ hoy, no domingo, inicio dentro de 08:00–18:00, fin (inicio + duración del servicio) ≤ 18:00, y sin hora ya pasada si es hoy; solape **por cada mecánico asignado** —si alguno está ocupado se rechaza la cita completa y el `AppointmentOverlapException` nombra al mecánico en conflicto— excluyendo `Cancelado`/`Finalizado`; guarda las filas de `CitaMecanico` dentro de una transacción<br>`CancelarCitaAsync(citaId)`<br>`ObtenerPorRangoAsync(inicio, fin)` (incluye `CitaMecanicos`) <br>`ObtenerPorClienteAsync(clienteId)` → `List<CitaDTO>`<br>`ObtenerPorVehiculoAsync(vehiculoId)` → `List<CitaDTO>` (historial completo del vehículo, más recientes primero; reutiliza `ProyeccionListado`)<br>**Listado/detalle:** `BuscarPaginadoAsync(filtro, pagina, tamanioPagina)` → `List<CitaDTO>`<br>`ContarAsync(filtro)` → `int` (comparte `AplicarFiltro` con el listado, para que el total coincida; el filtro por mecánico usa `CitaMecanicos.Any`)<br>`ObtenerDetallePorIdAsync(citaId)` → `CitaDetalleDTO?`<br>**Edición no-agenda:** `ActualizarCitaAsync(dto)` → `bool` (false si no existe; exige cita no cancelado/finalizado; valida servicio/vehículo existen, activos o los actuales (se permite conservar los inactivos actuales), vehículo del mismo cliente; recalcula `HoraFin` desde la duración del nuevo servicio, valida fin ≤ 18:00 y solape **por cada mecánico asignado** excluyendo `Cancelado`/`Finalizado`)<br>**Reprogramación:** `ReprogramarCitaAsync(dto)` → `bool` (false si no existe; exige cita no cancelado/finalizado; valida mecánicos activos y especialidad compartida; conserva el servicio y recalcula `HoraFin` con su duración; fecha ≥ hoy, no domingo, inicio 08:00–18:00, fin ≤ 18:00, sin hora pasada si es hoy; solape **por cada mecánico asignado** excluyendo la propia cita y `Cancelado`/`Finalizado`; **reemplaza** el conjunto de mecánicos) | Scoped |
| `ICalendarQueryService` | `CalendarQueryService` | `ObtenerEventosAsync(inicio, fin)` | Scoped |
| `IDashboardService` | `DashboardService` | `ObtenerResumenDiarioAsync(fecha)` → `ResumenDiarioDTO` (KPIs del panel `/`): una lectura de `ICitaService.ObtenerPorRangoAsync(dia, dia)` + `IMecanicoService.ContarActivosAsync()` + `IClienteService.ContarBusquedaAsync(null, Activos)` + `IVehiculoService.ContarActivosAsync()`. Calcula: total de citas y conteo por cada `EstadoCita` (orden del enum, incluyendo los que quedan en 0), listado ordenado por hora, mecánicos distintos con citas no canceladas, y ocupación = Σ(duración × mecánicos asignados) de las citas no canceladas ÷ (mecánicos activos × `HorarioAtencion.MinutosPorDia`); `Cerrado = true` los domingos. **`ObtenerResumenPeriodoAsync(inicio, fin)`** → `ResumenPeriodoDTO` para los gráficos del panel: `Conteos` por `EstadoCita` del período (incluye canceladas, orden del enum aunque queden en 0) y `CargaMecanicos` = mecánicos **activos** ordenados por minutos descendentes (los de 0 también, para ver quién está ocioso) sumando duraciones solo de citas **no canceladas**. DTOs en `Entidad/DTOs/Dashboard/` | Scoped |
| `INotificationService` | `NotificationService` | `NotificarCitaAgendadaAsync(cita)` | Scoped |
| `IAuthServices` | `AuthServices` | `LoginAsync(email, password)`<br>`RegisterAdminAsync(email, nombre, password)` (delega con `(null, Nacional)`)<br>`RegisterAdminAsync(email, nombre, password, cedula?, tipoIdentificacion)` (normaliza per-tipo, persiste `Cedula` + `TipoIdentificacion`)<br>`EmailExisteAsync(email)` | Scoped |
| `IAdministradorService` | `AdministradorService` | `ObtenerPerfilAsync(adminId)` → `PerfilAdminDTO?` (proyección `AsNoTracking`: Nombre, Cedula, TipoIdentificacion, `Email`→`Correo`, CorreoNotificaciones)<br>`ActualizarPerfilAsync(adminId, dto)` → `bool` (false si el admin no existe). Persiste Nombre (`Trim`), TipoIdentificacion y Cedula (`Formatos.NormalizarIdentificacion` según el tipo, vacío→`null`, `InvalidOperationException` con el mensaje del tipo si no cuadra) y CorreoNotificaciones (`Trim` + vacío→`null`; unicidad entre admins case-insensitive excluyendo al propio → `InvalidOperationException`). Pensado para cuando el puesto de administrador global pasa a otra persona.<br>`CambiarPasswordAsync(adminId, dto)` → `bool` (false si el admin no existe; verifica la actual con `PasswordHasher.Verify` + fallback legado sin `$`; rechaza nueva == actual; rehashea con `PasswordHasher.Hash`) | Scoped |
| `AuthenticationStateProvider` | `CustomAuthenticationStateProvider` | Revalida el estado de auth contra BD cada 10 min (hereda de `RevalidatingServerAuthenticationStateProvider`) | Scoped |
| — | `DbSeeder` | `SeedAsync()` — siembra categorías, servicios, mecánicos, clientes, vehículos, citas, admin global | Scoped |

**Notas de comportamiento relevantes:**
- **Insignias de estado uniformes:** todas las insignias de estado de cita (`Citas`, `CitaDetalle`, `Clientes`, `ClienteDetalle`, `VehiculoDetalle`) usan la clase `.badge-estado` de `wwwroot/app.css` (ancho fijo 6.5rem y texto centrado); el texto proviene de `Formatos.NombreEstado` ("En Proceso" para `EstadoCita.EnProceso`) y el color de `Formatos.BadgeEstado` (mapeo estado → clase Bootstrap, centralizado para no duplicarlo por pantalla). El **donut del panel de control** usa `Formatos.ColorEstado`, espejo hex del mismo mapeo (`#6c757d/#0d6efd/#0dcaf0/#198754/#dc3545`), para que las porciones del gráfico no puedan divergir de las insignias: si cambia uno, cambia el otro.
- **Ficha de vehículo (`/vehiculos/{id}`):** página `[Authorize]` (Admin + Mecánico) con la información general del vehículo (placa, marca, modelo, año, kilometraje, color, notas, dueño y estado) y el historial completo de sus citas (todas, más recientes primero), cada una con enlace a `/citas/{id}`. Se llega desde cuatro puntos: botón **Ver** de la tabla de vehículos de `ClienteDetalle`, enlace de placa en el listado de `Citas`, tarjeta de vehículo de `CitaDetalle` y placa del panel expandible de `Clientes`. `Kilometraje` (`int?`), `Color` y `Notas` se editan en el modal de vehículo de `ClienteDetalle` y se persisten en `VehiculoService` (antes `Color`/`Notas` existían en la entidad pero no se poblaban desde la UI).
- **Editar vehículo desde la ficha (`/vehiculos/{id}`):** botón **Editar** visible solo para Administrador (`<AuthorizeView Roles="Administrador">`; los Mecánicos siguen en solo lectura). Abre el modal compartido `VehiculoFormModal` con los campos básicos (Placa, Marca, Modelo, Año, Kilometraje, Color, Notas) — **sin** Eliminar/Transferir — y guarda vía `VehiculoService.ActualizarAsync` (mismo `ActualizarVehiculoDTO`); al éxito recarga la ficha y muestra una alerta de confirmación. La reprogramación de citas (fecha/hora/mecánico) y los cambios de estado siguen siendo operaciones aparte.
- **Modal de vehículo compartido:** `Components/Shared/VehiculoFormModal.razor(.cs)` centraliza el formulario crear/editar vehículo. El dueño (`ClienteDetalle` o `VehiculoDetalle`) conserva el modelo y el estado (`guardando`, `mensajeError`) y aporta las acciones mediante `OnGuardar`/`OnCancelar`; `ClienteDetalle` además inyecta sus botones Eliminar/Transferir por la ranura `FooterIzquierdo`. Sin cambio de comportamiento en `ClienteDetalle`.
- **Reporte de citas por vehículo:** botón **Generar Reporte** en el encabezado del historial (visible solo si hay citas) que llama a `JS.InvokeVoidAsync("print")` (impresión del navegador / Guardar como PDF; sin librería ni JS propio). El reporte incluye un encabezado solo-impresión (`d-none d-print-block`: marca, vehículo y fecha de generación), la información general, el resumen por estado y el historial con insignias. El chrome (`breadcrumb`, encabezado de página, alertas, botones `d-print-none` y columna **Acciones**) se oculta al imprimir: `Components/Layout/MainLayout.razor.css` oculta `.sidebar`/`.top-row` y `wwwroot/app.css` neutraliza el tema (fuerza claro, aplana sombras y conserva el color de las insignias con `print-color-adjust: exact`).
- `ClienteDTO.CantidadVehiculos` es **condicional en la proyección**: cliente activo → cuenta solo vehículos `Activo = true`; cliente inactivo → cuenta **todos** los vehículos (coincide con la lista visible de `/clientes/{id}`, que usa `incluirInactivos: !cliente.Activo`). La proyección `c.Activo ? c.Vehiculos.Count(v => v.Activo) : c.Vehiculos.Count()` fue **verificada contra MySQL real**: EF Core 9 la traduce a SQL sin errores. El guard de `EliminarDefinitivoAsync` sigue contando **todos** los vehículos, así que un cliente inactivo con solo vehículos desactivados y 0 citas ahora **no** muestra el botón Eliminar (el conteo visible = total) — dirección segura.
- **Cascada de estado de vehículos**: `DesactivarAsync` desactiva solo los vehículos activos; `ReactivarAsync` reactiva **todos** (incluidos los que se habían desactivado individualmente antes). Sin flag `DesactivadoPorCascade` (YAGNI). En ambos casos las entidades están tracked y hay una sola `SaveChangesAsync` (transacción implícita de EF).
- **Transferencia de vehículos** (`TransferirAsync`): guards en orden — vehículo nulo → `false`; mismo dueño → `InvalidOperationException("El vehículo ya pertenece a este cliente.")`; destino nulo/inactivo → `"El cliente destino no está activo."`; citas futuras (`Fecha >= hoy`, estado no Cancelado/Finalizado) → `"El vehículo tiene N citas futuras. Cancelelas antes de transferirlo."`. Al guardar: `ClienteId = nuevo` + `Activo = true` (la transferencia reactiva). Las citas pasadas conservan el `ClienteId` anterior.
- El botón Eliminar de vehículo vive en el footer del modal de edición (no hay botones de acción por encima de lo heredado en la tabla); el confirm de borrado reemplaza al formulario sin apilar modales, e igual hace el botón **Transferir** (cierra el formulario y abre el modal de transferencia en su lugar). `VehiculoService` usa `NormalizarYValidar` + duplicado sobre todas las filas, patrón espejo de `ClienteService`.
- La advertencia de citas futuras al desactivar se cuenta **client-side**: `ClienteDetalle` usa la lista `citas` ya cargada; `Clientes` hace un `CitaService.ObtenerPorClienteAsync` al abrir el modal (con `catch` que silencia la advertencia si el conteo falla).
- **`PasswordHasher` estático** (`Logica/Utilidades/PasswordHasher.cs`): PBKDF2-SHA256, 310k iteraciones, salt 16B, hash 32B, formato `{saltBase64}${hashBase64}`. Fuente única de verdad: `AuthServices.HashPassword` y `VerifyPassword` delegan en él (la extracción usa el método estático `Rfc2898DeriveBytes.Pbkdf2`, por lo que las 3 advertencias SYSLIB0060 del código anterior desaparecieron). `DbSeeder` sigue usando `AuthServices.HashPassword`. `VerifyPassword` conserva la compat legado: un hash sin `$` se trata como texto plano y loggea warning.
- **CRUD de mecánicos (backend)**: `MecanicoService.CrearAsync`/`ActualizarAsync` normalizan email (trim + `ToLowerInvariant`, invariante: siempre minúsculas), verifican unicidad **manual** contra `Administradors` y `Mecanico` (`ExisteEmailAsync`; no hay índice único en la tabla), normalizan la cédula per-tipo vía `Formatos.NormalizarIdentificacion` (**opcional**: blanco → `null`) y persisten `TipoIdentificacion` (default `Nacional` en entidad y DTOs). Exigen que la especialidad exista, exigen `Password` mín. 6 + `[Compare]` en Crear, y en Actualizar `Password` vacío = conservar hash / parcialmente lleno = `InvalidOperationException`. `Telefono` es **obligatorio** y se normaliza a 8 dígitos (`NormalizarTelefono`), mismo invariante que `Cliente`. `DesactivarAsync` **no revisa citas futuras** (ver Sección 11).
- **Reglas de agenda compartidas UI + servicio (`/agendar`)**: semana visible lun–sáb (domingo cerrado), ventana 08:00–18:00 en pasos de 30 min, display 12 h (`h:mm tt` con `InvariantCulture`), UI en español (cultura `es-CR`), sin pasado. La UI las aplica en el tablero/modal (celdas pasadas no clicables, slots ocupados **por mecánico** deshabilitados desde `citasSemana` con `Cita.EstaActiva()`/`OverlapsWith()`, hora inicial por defecto que salta los tramos ocupados del mecánico); `CitaService.AgendarCitaAsync` las revalida como última barrera (ver fila `ICitaService`). La concurrencia es por mecánico: la misma hora queda libre para otro mecánico.
- **Horario de atención centralizado (`Entidad/Utilidades/HorarioAtencion.cs`)**: `Apertura` (8:00), `Cierre` (18:00), `MinutosPorDia` (600) y `EsDiaNoLaboral(fecha)` (domingo). Antes el `new TimeOnly(8, 0)` / `new TimeOnly(18, 0)` estaba escrito a mano en dos lugares de `CitaService` (agendar y editar); ahora ambos usan la constante, y el panel de control calcula la capacidad del día con los mismos valores, de modo que la UI y las guardas no pueden divergir.

### Servicios relacionados con usuarios/auth

**AuthServices** (`Logica/Servicios/AuthServices.cs`):
- **Patrón de hash**: PBKDF2-SHA256 vía `PasswordHasher` (`Logica/Utilidades/PasswordHasher.cs`), 310,000 iteraciones, salt de 16 bytes (128 bits), hash de 32 bytes (256 bits); formato `{saltBase64}${hashBase64}`.
- **Formato de salida**: `{saltBase64}${hashBase64}` (separado por `$`).
- **Verificación**: `PasswordHasher.Verify` con `CryptographicOperations.FixedTimeEquals`; el caso legado de contraseña en texto plano (sin `$`) lo sigue manejando `VerifyPassword` con warning.
- **Login**: Intenta primero en `Administrador`, si no encuentra o contraseña inválida, intenta en `Mecanico` con filtro `m.Activo`.
- **RegisterAdmin**: overload viejo `(email, nombre, password)` delega en el nuevo `(email, nombre, password, cedula?, tipo)` con `(null, Nacional)`. Crea `Administrador` con `Activo = true`, `Cedula` normalizada per-tipo (o `null`) + `TipoIdentificacion`, y hash de password. Retorna `false` si el email ya existe en `Administradors` o `Mecanico`.
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
- Las credenciales inválidas devuelven **401** (`Results.Unauthorized()`) sin cuerpo. `UseStatusCodePagesWithReExecute` está excluido para `/api/*` (ver `Program.cs`), de modo que ese 401 no se re-ejecuta contra el endpoint de componente `/not-found` (que intentaría leer el POST como formulario y lo convertiría en un 400 engañoso).
- Al autenticarse, `Login.razor.cs` vuelve a `?ReturnUrl` **solo si es una ruta local válida** (empieza con `/` y no con `//` ni `/\`); en cualquier otro caso navega a `/`. Evita open redirects.
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
@using Mecano.Components.Shared
@using Mecano.Entidad.Clases
@using Mecano.Entidad.DTOs
@using Mecano.Entidad.Utilidades
@using Mecano.Logica.Interfaces
@using ApexCharts
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
```

#### `MainLayout.razor`
```razor
@inherits LayoutComponentBase
@inject IJSRuntime JS
@inject NavigationManager NavigationManager
@inject AuthenticationStateProvider AuthenticationStateProvider
@inject ThemeState Tema

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
                    <div class="dropdown"> ... menú de usuario (nombre, email, rol, enlace "Mi Perfil" -> /perfil, logout vía HandleLogout) ... </div>
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

`@code`: `ToggleTheme` invoca `window.mecanoTheme.toggle` y refleja el resultado en `ThemeState` (`Tema.Set`) para que los gráficos del panel se re-creen al alternar el tema; `ToggleUserMenu`/`CloseUserMenu` controlan el dropdown de usuario (nombre, email, rol, logout); `OnAfterRenderAsync(firstRender)` sincroniza `ThemeState` con `window.mecanoTheme.isDark` (el tema inicial puede venir de `localStorage` o del SO), inicializa `window.mecanoRedirectFeedback` (definido en `auth.js`: oscurece el menú cuando hay redirect pendiente a login) y le pasa el estado de autenticación vía `setAuthenticated`. Ya **no** se llama a `window.mecanoTheme.syncIcons` desde el layout. `HandleLogout` conserva el flujo JS original (`window.authFetch.logout` a `/api/auth/logout` + `NavigateTo("/login", forceLoad: true)`). `RedirectToLogin.razor` invoca `window.mecanoRedirectFeedback.assignRedirectClass` antes de navegar a `/login`.

#### `LoginLayout.razor`

Layout anónimo (sin `NavMenu` ni sidebar) usado por `Login.razor` y `AccesoDenegado.razor` vía `@layout LoginLayout`. Conserva el encabezado con la marca MECANO y el toggle de tema, y el bloque `#blazor-error-ui`, pero omite el menú lateral y el dropdown de usuario. Su `@code` expone únicamente `ToggleTheme` (`window.mecanoTheme.toggle`); no inicializa `mecanoRedirectFeedback` porque en estas pantallas no hay menú que oscurecer.

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
                <i class="bi bi-speedometer2" aria-hidden="true"></i> Dashboard
            </NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="agendar">
                <i class="bi bi-calendar-plus" aria-hidden="true"></i> Agendar Cita
            </NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="citas">
                <i class="bi bi-calendar-event" aria-hidden="true"></i> Citas
            </NavLink>
        </div>

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
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="perfil">
                <i class="bi bi-person-circle" aria-hidden="true"></i> Mi Perfil
            </NavLink>
        </div>
    </nav>
</div>
```

> El header `Gestión` es un `<div>` literal **sin `<AuthorizeView>`**: los 4 enlaces los ve cualquier usuario autenticado (incluido `Mecanico`). La defensa queda en los atributos `[Authorize(Roles = ...)]` de cada página (por eso "Mi Perfil" aparece en el menú de un mecánico pero `/perfil` le responde "No tienes permisos"). Ver Sección 11.

### Tema "Industrial Clean" (variables CSS + light/dark)

- **Tokens en `wwwroot/app.css`**: `:root` define la paleta light (slate + acento cian-600 `#0284C7`); `[data-bs-theme="dark"], body.dark-mode` la versión dark (slate oscuro `#0F172A`/`#1E293B`, sidebar `#020617`, acento cian-400 `#38BDF8`, badges en contraste suave). Ambos bloques fijan `color-scheme` y definen `--text-inverse` (texto sobre superficies con acento saturado: blanco en light, `#0F172A` en dark — lo usa el hover de `.enlace-cascada`, porque en dark el acento es claro).
- **Token `--placeholder-color`** (añadido en `406871a`): `#4a4e69` en `:root` (light) y `#e5e5e5` en `[data-bs-theme="dark"]`. Lo consume `Components/Pages/Login.razor.css` para el placeholder del formulario de login, de modo que el texto atenuado del input se lee bien en ambos temas.
- **Tema**: `data-bs-theme` se aplica en `<html>` (Bootstrap 5.3.3 lo usa nativamente para tablas/modales/dropdowns/formularios); `body.dark-mode` se añade como gancho de respaldo. Script inline en `<head>` de `App.razor` aplica el tema guardado antes del primer paint (sin flash).
- **`theme.js`** (`window.mecanoTheme`): `toggle()`/`apply()`/`init()`/`syncIcons()`; persiste en `localStorage` bajo `mecano-theme`; `init()` cae a `prefers-color-scheme` si no hay valor guardado. `syncIcons` alterna `bi-moon-stars`/`bi-sun` en los elementos `.theme-toggle-icon`.
- **Badges de estado opt-in**: `.badge-status-success/-warning/-danger/-neutral` (BG/texto por token) — no sobre-escriben las utilidades `bg-*` de Bootstrap que ya usan Clientes/Detalle. Las vistas de Categorías/Servicios, Mecánicos y los conteos usan estos badges por ser theme-aware; las vistas de citas mantienen `bg-secondary` para el «Inactivo» histórico.
- **Sidebar del menú**: siempre oscuro en ambos temas (`var(--bg-sidebar)`); link activo con tinte `--nav-active-bg` + barra izq `--primary-accent` vía `box-shadow: inset 3px`; brand `MECANO` con `bi-gear-wide-connected`.
- **`.btn-primary`/**`a` globales** repuntan a los tokens de acento en vez de los colores hardcodeados de la plantilla.
- El **logout** del header no usa una ruta `/logout` (no existe): reutiliza el `HandleLogout` existente (fetch + navegación forzada).

### Páginas `.razor` (lista completa)

| Ruta (`@page`) | Autorización | Propósito |
|---|---|---|
| `/` | `[Authorize(Roles = "Administrador")]` | **Panel de control** (KPIs del día, InteractiveServer; reemplaza la Home de plantilla). Tarjeta **Citas de hoy**: total + desglose por estado en `nav-tabs` implementadas en Blazor puro (sin JS de Bootstrap; `PestanaActiva` + clase `active` + `aria-selected`), cada pestaña con su badge de conteo vía `Formatos.BadgeEstado` y panel con las citas de ese estado (hora, cliente, vehículo·placa, servicio; estado vacío con `alert-info`); arranca en la primera pestaña con citas. Tarjeta **Ocupación del día**: minutos-mecánico reservados ÷ (mecánicos activos × `HorarioAtencion.MinutosPorDia`), barra `role="progressbar"` con escala <50 % baja, 50–85 % óptima, >85 % alta (cubre también >100 %, posible si mecánicos inactivos conservan citas); domingo → «Taller cerrado», sin mecánicos activos → «no hay capacidad calculable». Tarjeta **Taller y personal**: mecánicos activos (y cuántos tienen citas hoy), clientes y vehículos activos. **Fila de gráficos** (ApexCharts, debajo de los KPIs, 6+6): **Distribución por estado** (donut con total en el centro, colores de `Formatos.BadgeEstado` vía `Formatos.ColorEstado`, leyenda visible con color·conteo·% que además es el equivalente en texto del gráfico) y **Carga por mecánico** (barras horizontales de **tiempo asignado en minutos enteros**, eje de valor con marcas «45 min / 1 h / 2 h 30 min», tooltip «Tiempo asignado: 1 hora 30 minutos» —formatters JS espejo de `Formatos.Duracion`, nunca «1.5»—, y detalle «N citas · duración» debajo; solo citas no canceladas). Toggle **Semana/Mes** (default **Mes**) repetido en ambas cabeceras apuntando al mismo estado; vacío con datos → `alert-info`, error → alerta con Reintentar; alturas/colores según tema (`Theme.Mode` + `ForeColor` explícitos, ApexCharts no lee variables CSS) y `@key="Tema.IsDark"` recrea ambos charts al alternar el tema (estado compartido vía `ThemeState`, sincronizado por `MainLayout` en el toggle y en el primer render). Enlaces «Ver todas las citas» y «Clientes activos» con la clase `.enlace-cascada` (efecto cascada + cristal). Botón de recarga (⟳) refresca día **y** período |
| `/agendar` | `[Authorize(Roles = "Administrador")]` | Tablero mecánicos×días lun–sáb + modal "mecánico primero" (InteractiveServer). Advierte visualmente cuando el cliente buscado/seleccionado está inactivo (fila gris + badge en resultados, tarjeta y resumen del modal en tonos warning, ícono en el chip de la cita). Muestra también los **mecánicos inactivos** que tengan citas no canceladas en la semana visible (fila atenuada + badge `Inactivo`); sus celdas no son clicables y no ofrecen "+ Agendar" |
| `/citas` | `[Authorize(Roles = "Administrador")]` | Listado paginado de citas con filtros (rango de fechas, mecánico, estado, cliente/placa) y paginación numerada. Marca con badge `Inactivo` las citas de clientes inactivos y con la insignia «Servicio inactivo» las citas cuyo servicio fue desactivado. Enlaza al detalle (InteractiveServer) |
| `/citas/{Id:int}` | `[Authorize(Roles = "Administrador")]` | Detalle de cita: programación (fecha/horario/duración/estado), cliente, vehículo, servicio, mecánico y notas. La tarjeta de cliente resalta en tonos warning con badge `Inactivo` y un aviso si el cliente está inactivo; la tarjeta de mecánico muestra badge `Inactivo` si el mecánico está inactivo, y la tarjeta de servicio pasa a `border-warning` con insignia `Inactivo` + `alert-warning` si el servicio fue desactivado (no admite citas nuevas; la cita se conserva). Botón **Editar** (modal: vehículo, servicio y notas; oculto si la cita está cancelada o finalizada); **Reprogramar** (modal: fecha/hora/mecánico; solo lista mecánicos activos, con aviso de reasignación si el actual está inactivo) (InteractiveServer) |
| `/categorias` | `[Authorize(Roles = "Administrador")]` | Lista de categorías con CRUD modal, rediseñada al **patrón Clientes**: `<h1>` + alertas + una `.card.shadow-sm` con búsqueda en el `card-header` (nombre o descripción, en memoria) y «Nueva categoría». Tabla Nombre/Descripción/Servicios/Mecánicos/Acciones con insignias theme-aware para los conteos; acciones en línea Ver/Editar/Eliminar. Sin filtro de Estado (`CategoriaDTO` no tiene `Activo`) |
| `/categorias/{Id:int}` | `[Authorize(Roles = "Administrador")]` | Servicios de una categoría, también al patrón Clientes: breadcrumb + `<h1>` con «Volver», card con búsqueda + **Estado** (`FiltroActivo`: Activos/Inactivos/Todos) + «Nuevo servicio». Tabla Nombre/Descripción/Duración/Precio/Estado/Acciones; filas inactivas atenuadas. La columna Duración se muestra legible (`Formatos.Duracion`) y el precio con `Formatos.Moneda`; el modal crear/editar muestra hints en vivo bajo los `InputNumber`. **Desactivar/Reactivar** servicio con modal de confirmación (baja lógica vía `EliminarAsync`/`ReactivarAsync`); un servicio desactivado no se ofrece al agendar y las citas que lo usan se señalan. Los `InputNumber` del modal **no** llevan `min`/`max`/`step` nativos para que la validación del navegador no bloquee el `submit` (las `ValidationMessage` vendrán de DataAnnotations) |
| `/mecanicos` | `[Authorize(Roles = "Administrador")]` | Lista de mecánicos rediseñada al **patrón Clientes**: `<h1>` + alertas + una `.card.shadow-sm` con búsqueda en el `card-header` (nombre, cédula —cruda o formateada—, correo o especialidad; en memoria) + **Estado** (`FiltroActivo`: Activos/Inactivos/Todos) + «Nuevo mecánico» (deshabilitado mientras no haya especialidades cargadas) y `card-footer` con el conteo. Tabla Identificación/Nombre/Email/Teléfono/Especialidad/Estado/Acciones con insignias theme-aware (`badge-status-success`/`badge-status-neutral`) y filas inactivas atenuadas; teléfono e identificación se presentan con `Formatos.Telefono`/`Formatos.FormatearIdentificacion`. Modal crear/editar (credenciales + selector de TipoIdentificacion + especialidad; en edición la contraseña vacía conserva el hash) y **modales de confirmación** para desactivar y reactivar |
| `/clientes` | `[Authorize]` | Lista de clientes con búsqueda, paginación y selector de estado **Activos/Inactivos/Todos** (`FiltroActivo`, sin faceta por tipo). Lectura para ambos roles; escritura tras `AuthorizeView Roles="Administrador"`. La búsqueda por cédula es solo para Administrador (el Mecánico busca por nombre o placa; ver Sección 11). Modal crear/editar con selector de TipoIdentificacion; tabla con identificación formateada + tipo. Desactivar muestra warning de citas futuras (fetch al abrir el modal); **Reactivar ahora tiene modal de confirmación** con warning de vehículos en cascada. Cada fila es expandible (clic en la fila o en el chevron) y despliega un panel con los vehículos y citas del cliente, con la placa destacada (acordeón de una fila, carga diferida; ver Sección 11) |
| `/clientes/{Id:int}` | `[Authorize]` | Detalle de cliente: datos (identificación formateada por tipo + campo de tipo), vehículos (fila inactiva con badge `Inactivo`) y citas. Modal editar con selector de TipoIdentificacion. CRUD de vehículos + **Transferir a otro cliente** (footer del modal de edición, dos vistas buscar↔confirmar en un solo modal con debounce 400 ms, top-10, solo activos). Desactivar con warning de citas futuras; **Reactivar con modal de confirmación** y warning de cascada. Escritura solo tras `AuthorizeView Roles="Administrador"` |
| `/login` | `[AllowAnonymous]` | Login con EditForm + `window.authFetch` (InteractiveServer) |
| `/acceso-denegado` | `[AllowAnonymous]` | Página de acceso denegado |
| `/not-found` | Ninguna | Página de error 404 |
| `/Error` | Ninguna | Página de manejo de errores |
| `/connectiontest` | `[Authorize(Roles = "Administrador")]` | Prueba de conexión a la BD (InteractiveServer) |
| `/perfil` | `[Authorize(Roles = "Administrador")]` | "Mi Perfil" del administrador autenticado (InteractiveServer). Avatar circular con las iniciales de `Nombre` + badge `Administrador Global` (derivado del claim `EsAdminGlobal`). **Editables**: Nombre, Tipo de identificación, Identificación (`InputSelect` + `InputText`; la identificación puede quedar vacía y el servicio la normaliza/valida por tipo) y `CorreoNotificaciones` (opcional: vacío lo borra; unicidad case-insensitive validada en el servicio). **Solo lectura**: el correo de inicio de sesión (es la clave de autenticación). Pensado para cuando el puesto de administrador global pasa a otra persona. Tarjeta aparte para **cambiar la contraseña**, con los tres campos bajo el mismo patrón del login (`type="text"` + `autocomplete="off"` + `-webkit-text-security` + botón de ojo) para que el navegador no ofrezca guardar la contraseña en las computadoras compartidas del taller |

> Nota: `Counter.razor` y `Weather.razor` (páginas de plantilla) fueron eliminadas del proyecto.

### Componentes compartidos en `Shared/`
- `Components/Shared/RedirectToLogin.razor` — redirige a `/login` cuando `NotAuthorized` detecta usuario anónimo.
- `Components/Shared/ThemeState.cs` — estado del tema (`IsDark` + evento `Changed`) compartido en el circuito: `MainLayout` lo actualiza en el toggle y en el primer render; `Home.razor.cs` se suscribe (`IDisposable`) y fuerza el re-render de los gráficos con `@key="Tema.IsDark"`.
- Los componentes de layout están en `Components/Layout/`.

### Render mode configurado
- **Predominante**: `InteractiveServer` — la mayoría de páginas tienen `@rendermode InteractiveServer` (Home/panel de control, AgendarCita, Login, ConnectionTest, Categorias, CategoriaDetalle, Mecanicos).
- `App.razor` usa `.AddInteractiveServerRenderMode()` en Program.cs.
- No hay configuración `WebAuto` o `WebAssembly` explícita más allá del server.

### Página de login, register o account
- **Login implementado**: `Components/Pages/Login.razor` (`@page "/login"`, InteractiveServer, `[AllowAnonymous]`) con `EditForm` y `DataAnnotationsValidator` sobre `LoginDTO`. Al autenticarse correctamente hace `NavigateTo(ReturnUrl, forceLoad: true)`, donde `ReturnUrl` se enlaza con `[SupplyParameterFromQuery]` y solo se usa si `EsRutaLocal` lo acepta (ruta relativa local); si no, navega a `/`. Los dos inputs (`email` y `password`) llevan las clases `input-email`/`input-password`, que hoy **no** usa ninguna regla de CSS (el placeholder se resuelve con `::deep input::placeholder`); son ganchos disponibles si se quiseren estilizar por campo. El campo de contraseña **no** usa `type="password"`: es `type="text"` con `autocomplete="off"` y el ocultado se hace por CSS (`-webkit-text-security: disc` alternado con `none`), con un botón de ojo dentro de un `input-group` para mostrar/ocultar; así el navegador no ofrece guardar la contraseña en las computadoras compartidas del taller.
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
- `UseStatusCodePagesWithReExecute("/not-found")` se registra con `app.UseWhen(... !Path.StartsWithSegments("/api"))`: las rutas del sitio siguen re-ejecutando a `/not-found` (404/403, etc.), pero las respuestas de `/api/*` conservan su propio código (p. ej. el 401 del login).

### Atributos `[Authorize]` en el código
- `[Authorize]` en `Clientes.razor` y `ClienteDetalle.razor`. `Home.razor` (panel de control) usa `[Authorize(Roles = "Administrador")]`.
- `[Authorize(Roles = ...)]` con la constante `Mecano.Entidad.Constantes.Roles.Administrador` en `AgendarCita.razor:2` y `ConnectionTest.razor:2`; literal `"Administrador"` en `Categorias.razor` y `CategoriaDetalle.razor`.
- `[AllowAnonymous]` en `Login.razor` y `AccesoDenegado.razor`.
- **Autorización granular en las páginas de Clientes**: ambas usan `[Authorize]` simple, así que un `Mecanico` puede consultar clientes. Los controles de escritura (crear, editar, desactivar, reactivar, eliminar, transferir vehículos y sus modales) están envueltos en `<AuthorizeView Roles="Administrador">` dentro del `.razor`. Además, la identificación del cliente (cédula + tipo) se muestra solo tras `<AuthorizeView Roles="Administrador">` en la tabla de `Clientes.razor` y en la tarjeta de datos de `ClienteDetalle.razor`; un `Mecanico` no la ve (ver Sección 11).

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
- **PBKDF2** (Password-Based Key Derivation Function 2), vía `PasswordHasher` (`Logica/Utilidades/PasswordHasher.cs`, método estático `Rfc2898DeriveBytes.Pbkdf2` con SHA-256). `AuthServices.HashPassword`/`VerifyPassword` delegan en él.
- **Parámetros**: 310,000 iteraciones, salt de 16 bytes, hash de 32 bytes, SHA-256.
- **Formato de stored password**: `{saltBase64}${hashBase64}` (ej. `odU4QXZ3W2l0aWQ9...`).

### Constantes de roles definidas
- **Constantes `SALTSIZE = 16`, `HASHSIZE = 32`, `Iterations = 310000`** en `PasswordHasher.cs` (antes en `AuthServices.cs`; la extracción eliminó las 3 advertencias SYSLIB0060).
- **Clase estática `Roles`** en `Entidad/Constantes/Roles.cs` con `Administrador` y `Mecanico`, usada en `Program.cs` (políticas) y en atributos `[Authorize(Roles = ...)]`.

### Tabla de "qué hace cada rol" (deducción del código)

| Rol | Lo que puede hacer (basado en comentarios XML + atributos/servicios) |
|---|---|
| **Administrador** | - Único rol que puede crear/reeschedular/cancelar citas (comentario en `Administrador.cs:6-8`).<br>- Puede registrar nuevos administradores via `RegisterAdminAsync`.<br>- Acceso a todas las entidades en BD (tabla `Administradors`).<br>--- | |
| **Mecanico** | - Puede ver citas disponibles por especialidad y horario.<br>- No puede agendar si está inactivo (`InactiveMechanicException`).<br>- Puede tener citas asignadas (`Mecanico.CitaMecanicos` navigation).<br>- Login requiere `m.Activo` (AuthServices:104).<br>- No tiene permisos de administración pura. | |

**Ya implementado**: El middleware ASP.NET Core (cookies + claims + `AuthorizeRouteView` + políticas) protege rutas por rol. Los roles se distinguen en el login por el tipo de entidad encontrado (`Administrador` vs `Mecanico` activo), y `AuthenticatedUser` ahora incluye `esAdminGlobal` como quinto campo del record.

---

## 10. Convenciones del proyecto

| Convención | Observación |
|---|---|
| **Espaciado de nombres** | `Mecano` (raíz), `Mecano.Entidad.Clases`, `Mecano.Entidad.DTOs`, `Mecano.Entidad.DTOs.Categoria`, `Mecano.Entidad.Constantes`, `Mecano.Data`, `Mecano.Logica.Interfaces`, `Mecano.Logica.Servicios`, `Mecano.Components`, `Mecano.Components.Layout`, `Mecano.Components.Pages.Categoria`, `Mecano.Components.Pages.Citas` |
| **Idioma de identificadores** | Mixto: nombres de clase y propiedades en **inglés** (Email, Password, Nombre, Categoria, Service, etc.), pero comentarios y mensajes de usuario en **español** (errores, placeholders, alerts). |
| **Idioma de comentarios** | Principalmente **español** (resumen de clases, comentarios XML), con algunos bloques de código en inglés (`using System.ComponentModel.DataAnnotations`). |
| **Nombres de personas capitalizados** | Todo nombre de cliente o mecánico que se muestra en pantalla lleva `style="text-transform:capitalize;"` inline, para que un nombre guardado en minúsculas se vea "Juan Pérez" sin alterar el dato persistido. Aplica a tablas, tarjetas, chips, modales, `<option>` y encabezados; **no** a `title`/`aria-label`/`<PageTitle>` (no renderizan estilos). Sitios cubiertos: `Clientes.razor`, `ClienteDetalle.razor` (incluye autores de comentarios), `Mecanicos.razor`, `AgendarCita.razor`, `Citas.razor`, `CitaDetalle.razor`. |
| **Patrón de nombres para archivos `.razor`** | `PascalCase` para nombres de archivo (Home.razor, AgendarCita.razor, CategoriaDetalle.razor) con code-behind `.razor.cs` en los CRUD (Categorias, CategoriaDetalle). Las rutas `@page` usan minúsculas con guiones/segmentos (`/agendar`, `/connectiontest`, `/categorias/{Id:int}`). |
| **Nullable reference types** | `<Nullable>enable</Nullable>` en csproj — habilitado globalmente. Las entidades usan `string?` y `int?` donde es apropiado. |
| **Uso de `ImplicitUsings`** | `<ImplicitUsings>enable</ImplicitUsings>` — las directivas `global using` se generan automáticamente por el SDK. Los using explícitos en `_Imports.razor` y archivos individuales son redundantes pero presentes por costumbre. |

---

## 11. Riesgos y observaciones (solo hechos)

| Riesgo/Observación | Detalle |
|---|---|
| **Archivos muy grandes** | `ClienteDetalle.razor` (**802 líneas**, code-behind **659**) es ahora el más grande; le sigue `AgendarCita.razor` (**795 líneas** + `AgendarCita.razor.css` de 339 aparte). **Deuda parcialmente saldada**: el formulario crear/editar vehículo se extrajo a `Components/Shared/VehiculoFormModal.razor` (reutilizado por `ClienteDetalle` y `VehiculoDetalle`); queda pendiente extraer `TransferirVehiculoModal.razor` (reevaluar por YAGNI/KISS). |
| **TODOs explícitos en el código** | Los DTOs `ActualizarServicioDTO.cs` y `ActualizarCategoriaDTO.cs` llevan `// TODO: remove (no-op on value types)` en la propiedad `Id`. |
| **`CategoriaDTO.CantidadServicios` cuenta servicios inactivos** | Suma todos los `Servicio` asociados (sin filtrar `Activo`). La vista de detalle usa `servicios.Count` del listado filtrado para mostrar el total visible. |
| **Colisión de namespace `Categoria`** | `Mecano.Entidad.DTOs.Categoria` (namespace) se cruza con `Mecano.Entidad.Clases.Categoria` (clase). Los DTOs de Servicio se mantienen planos en `Mecano.Entidad.DTOs` para evitar la misma colisión con la clase `Servicio`. |
| **Colisión de namespace `Cliente`** | `Mecano.Entidad.DTOs.Cliente` (namespace) frente a `Mecano.Entidad.Clases.Cliente` (clase). Compila porque los nombres de tipo difieren (`ClienteDTO` vs `Cliente`) y ambos `using` coexisten. |
| **Namespace de las páginas de Clientes es plural** | `Components/Pages/Cliente/` con `namespace ...Pages.Cliente` **rompía la compilación**: dentro de `Mecano.Components.Pages`, la referencia `Cliente` en `AgendarCita.razor:325` (`private Cliente? selectedCliente`) pasaba a resolverse contra el namespace hermano y lanzaba `CS0118: 'Cliente' es espacio de nombres pero se usa como tipo`. Se renombró la carpeta y el namespace a `Clientes`. Las rutas `/clientes` y `/clientes/{Id:int}` no cambian. La misma trampa aplica a `Categoria`, que ya usa `Pages/Categoria` — hoy no muerde porque ninguna página de `Pages/` declara una variable de tipo `Categoria`. |
| **`AuthorizeView` anidado dentro de `EditForm`** | Ambos componentes usan un parámetro de contenido llamado `context`, así que un `<AuthorizeView><EditForm>` directo produce `RZ9999` por ambigüedad. Todas las páginas nuevas llevan `Context="authCtx"` en el `AuthorizeView`. |
| **`IDisposable` en componentes Blazor** | `Clientes.razor.cs` y `ClienteDetalle.razor.cs` declaran `public partial class ... : ComponentBase, IDisposable` (ambas solo para CTS de debounce); no debe añadirse además `@implements IDisposable` en el `.razor`, o el `Dispose` queda ambiguo (RZ9999). |
| **Cascade delete desde Cliente** | `MySQLDBContext` declara `Cascade` hacia `Cita` y `Vehiculo`. Por eso `ClienteService` expone `DesactivarAsync` (soft) como operación normal de borrado, y `EliminarDefinitivoAsync` (hard) rechaza la operación si hay vehículos o citas asociadas. |
| **Firmas legadas de `IClienteService`** | `BuscarClientesAsync` y `ObtenerPorIdAsync` devuelven la entidad `Cliente`, no un DTO (legado: hoy solo `BuscarClientesAsync` se consume en UI, en `AgendarCita.razor:401`; `ObtenerPorIdAsync` ya no la llama ninguna página). Las lecturas por DTO son `ObtenerTodosAsync` y `ObtenerDetallePorIdAsync`. `RegistrarAsync` se **eliminó** por ser código muerto. |
| **Filtro de búsqueda no trivial** | `ClienteService.AplicarFiltro` usa `EF.Functions.Like` con comodines `%termino%`; así un término con `%` o `_` se interpreta como comodón. `ContarBusquedaAsync` reutiliza el mismo helper para que el conteo coincida con el listado paginado. |
| **`Mecanico.Cedula` ahora normalizada por tipo** | Desde el commit `8ec693b`, `Mecanico.Cedula` (y `Administrador.Cedula`) pasan por `NormalizarIdentificacion`: Nacional/DIMEX se guardan solo-dígitos, Pasaporte con `Trim()`. La cédula del mecánico es opcional (`null` si blanco). Solo `Cliente` tiene índice único de identificación; en mecánicos/admins la unicidad real es por email. El `Telefono` del mecánico es obligatorio y se normaliza a 8 dígitos (mismo invariante que `Cliente.Telefono`). |
| **`StringLength`/regex del DTO: identificación vs. nombre/correo** | `CrearClienteDTO`/`ActualizarClienteDTO.CedulaIdentidad` y `CrearMecanicoDTO`/`ActualizarMecanicoDTO.Cedula` aceptan hasta 20 caracteres a propósito: una cédula válida con guiones ocupa 10+ antes de normalizar y un DIMEX hasta 12; la regla estricta por tipo vive solo en `NormalizarIdentificacion` (el error llega por `catch (InvalidOperationException)`). En cambio, nombre y correo **sí** llevan `[RegularExpression]` a nivel de DTO (vía `Patrones`), porque su regla es única y no depende del tipo; `[RegularExpression]` trata null/vacío como válido, así que no duplica el error de `[Required]`. Los 4 DTOs traen `TipoIdentificacion = Nacional` por defecto. |
| **Código duplicado evidente** | El patrón de `using var context = await _factory.CreateDbContextAsync();` y `VerifyPassword`/`HashPassword` repetitivo en `AuthServices`. Los métodos de servicio siguen patrón similar (crear contexto, consultar, guardar). |
| **`catch` genérico** | `AuthServices.VerifyPassword` retorna `false` ante cualquier excepción (línea ~72-78) — patrón riesgoso pero intencional para no filtrar detalles. |
| **`Vehiculo.Activo` sin inicializador de entidad** | La entidad declara `public bool Activo { get; set; }` (default `false`) y la migración `20261006160916` no fija `defaultValue`. Hoy es correcto porque los **tres** puntos de creación (`VehiculoService.CrearAsync`, `CitaService.AgendarCitaAsync` y `DbSeeder`) asignan `Activo = true` explícito; un futuro `new Vehiculo { ... }` que lo olvide quedaría invisible en listas y selects. |
| **Creación rápida de vehículo al agendar (eliminada)** | `AgendarCitaDTO` ya no expone `NuevoVehiculo` y `CitaService.AgendarCitaAsync` ya no crea vehículos: exige un `VehiculoId` existente. Esto elimina la deuda de placa duplicada sin chequeo (antes `DbUpdateException` cruda) y hace que toda creación de vehículos pase por `VehiculoService` (normalización + duplicado). La UI de `AgendarCita` ya sólo permitía elegir vehículos del cliente. |
| **Soft delete de vehículo es invisible (sin reactivar directa)** | No hay `ReactivarAsync` ni toggle de "ver inactivos" (YAGNI): el vehículo desaparece de la lista y del select de agendar. **Excepción**: en el detalle de un cliente inactivo se listan los vehículos inactivos (fila con badge). Un vehículo inactivo puede volver a activo por dos caminos laterales: transferirlo (siempre guarda `Activo = true`) o reactivar su cliente (cascada). Su placa sigue ocupada (índice global) mientras exista. |
| **Reactivación en cascada sin flag `DesactivadoPorCascade`** | `ReactivarAsync` activa TODOS los vehículos del cliente, incluidos los que se habían desactivado **individualmente** antes de desactivar el cliente. Es el comportamiento aprobado en el ticket; el coste es que un vehículo desactivado a propósito "revive" al reactivar el cliente. |
| **`SinHistoria` vs guard de eliminación definitiva** | `ClienteDTO.CantidadVehiculos` es condicional (activo → solo vehículos activos; inactivo → todos), pero `EliminarDefinitivoAsync` exige 0 vehículos **en total**. El caso que queda: un cliente **activo** con todos sus vehículos desactivados muestra el botón Eliminar (conteo visible 0) y el servicio lo rechaza con mensaje claro. Para clientes inactivos el conteo visible ya es el total, así que el botón ni siquiera aparece. Dirección segura. |
| **La reactivación ganó modal de confirmación (cambio de UX)** | Antes del ticket, "Reactivar" en ambas páginas ejecutaba directo. Ahora muestra un modal con warning ("Al reactivar este cliente, TODOS sus vehículos (N) se reactivarán también."), coherente con el texto del ticket que pide confirmación explícita. Justificación documentada en el ticket (Nota 1). |
| **`IDisposable` solo por debounce** | `ClienteDetalle.razor.cs` implementa `IDisposable` únicamente para cancelar el CTS del debounce de búsqueda de destino (con comentario explicándolo). No debe añadirse `@implements IDisposable` en el `.razor` (trampa RZ9999 ya documentada). Alternativa descartada: botón "Buscar" en vez de debounce. |
| **Proyección condicional verificada contra BD** | `CantidadVehiculos = c.Activo ? c.Vehiculos.Count(v => v.Activo) : c.Vehiculos.Count()` se ejecutó contra MySQL real (checker temporal con servicios reales, datos de prueba eliminados al final): EF Core 9 la traduce sin errores y los conteos coinciden con las listas visibles. |
| **Parseo del `InputNumber` de precio depende de la cultura del circuito** | El display de precios es es-CR fijo vía `Formatos.Moneda`, pero `InputNumber` parsea con `CultureInfo.CurrentCulture` del circuito (hoy = cultura del SO/servidor; no hay `AddLocalization`/`UseRequestLocalization` en `Program.cs`). En un servidor con cultura `en-US`, escribir `35000,50` fallaría el parseo (`35000.50` sí funcionaría). **Fuera de alcance del ticket de formato de display** (decisión explícita); si algún día se necesita, agregar localización de requests en `Program.cs`. |
| **Colisión de namespace `Mecanico`** | El nuevo namespace `Mecano.Entidad.DTOs.Mecanico` (los 3 DTOs nuevos) se cruza con la clase `Mecano.Entidad.Clases.Mecanico` — misma trampa documentada con `Categoria` y `Cliente` en Sección 11. `HorarioDisponibleDTO.cs` lo resuelve con un alias explícito: `using MecanicoClase = Mecano.Entidad.Clases.Mecanico;`. Toda página/servicio que importe ambos namespaces debe usar el mismo alias para no romper la compilación (`CS0118`/ambigüedad). |
| **CRUD de mecánicos: backend + UI + tipos de identificación** | Resuelto y extendido en `8ec693b`: `Mecanicos.razor(.cs)` (`/mecanicos`, `[Authorize(Roles = "Administrador")]`) con selector de `TipoIdentificacion` (Nacional/DIMEX/Pasaporte) en crear/editar y columna de identificación formateada por tipo; `MecanicoDTO` expone `Cedula` + `TipoIdentificacion`. Quedan como deuda: el guard de citas futuras en `DesactivarAsync` (más abajo) y el smoke test manual de la UI con los 3 tipos. |
| **`MecanicoService.DesactivarAsync` no verifica citas futuras** | A diferencia del comportamiento esperado para clientes (warning de citas futuras), el desactivado de mecánico solo baja `Activo` (las citas existentes conservan sus filas de `CitaMecanico`). Deuda conocida del ticket: decidir si agregar warning/conteo como en cliente. |
| **SYSLIB0060 eliminado** | La extracción a `PasswordHasher` (método estático `Rfc2898DeriveBytes.Pbkdf2`) eliminó las 3 advertencias SYSLIB0060 que tenía `AuthServices.cs`; hoy la build queda solo con las 3 preexistentes de `CategoriaService`. |
| **Migraciones duplicadas de `TipoIdentificacion` (Mecanico/Admin)** | `20261011040945` agrega `TipoIdentificacion DEFAULT 0` a `Mecanico` + `Administradors`, y `20261011150853` agrega **otra vez** `TipoIdentificacion DEFAULT 1` solo a `Mecanico`. Inofensivo hoy porque el runtime usa `EnsureCreatedAsync` (las migraciones nunca se aplican); pero un `dotnet ef database update` fallaría en la segunda por columna duplicada, y el `DEFAULT 0` de la primera no corresponde a ningún miembro del enum (rango válido 1–3). Si algún día se migra a `MigrateAsync`, hay que colapsar/squash el historial. |
| **Comentarios "Fase 1 solo Nacional" obsoletos** | `TipoIdentificacion.cs:5-6`, `ClienteDTO.cs:6-7` y `MySQLDBContext.cs:80` todavía dicen que solo Nacional está expuesto o que los demás tipos son "Fase 2". Falso desde `8ec693b`: los 3 tipos están en UI, servicios y `Formatos`. Solo comentarios — actualizar al tocar esos archivos. |
| **`CedulaNacional` hardcodeado en 5 sitios (ignora el tipo)** | `AgendarCita.razor:67,83`, modal eliminar de `Clientes.razor:398`, y eliminar (`ClienteDetalle.razor:461`) + destinos (`:609`) y confirmación (`:624`) de transferencia usan `CedulaNacional` en vez de `FormatearIdentificacion`. Para DIMEX el fallback devuelve dígitos crudos (sin guiones erróneos); el caso borde es un pasaporte cuyos dígitos extraídos sumen exactamente 9 (ej. `AB123456789`), que se mostraría como `1-2345-6789` en esos sitios. Deuda menor: centralizar en `FormatearIdentificacion`. |
| **`RegistrarAsync` eliminado** | El contrato legado `RegistrarAsync(cedula, nombre, telefono, correo)` de `IClienteService`/`ClienteService` se eliminó: ninguna página lo llamaba (el CRUD usa `CrearAsync`) y forzaba `TipoIdentificacion.Nacional`. Si se necesitara una vía legacy, reimplementarla delegando en `CrearAsync` con el tipo explícito. |
| **`syncIcons` huérfano en `theme.js`** | `window.mecanoTheme.syncIcons` sigue definido pero `MainLayout` ya no lo invoca (el `OnAfterRenderAsync` hoy inicializa `mecanoRedirectFeedback`). El ícono luna/sol se rige por los atributos `data-on`/`data-off`. O eliminarlo o re-engancharlo; hoy es código muerto inofensivo. |
| **Refactor de layout "Industrial Clean" (CSS variables + tema light/dark)** | `app.css` declara los tokens `:root` (light) y `[data-bs-theme="dark"], body.dark-mode` (dark). `wwwroot/js/theme.js` (`window.mecanoTheme`) aplica/toggle/persiste en `localStorage` (clave `mecano-theme`); script inline en `<head>` de `App.razor` evita el flash al cargar. El toggle vive en el header (`MainLayout`, botón `.theme-toggle-btn` con ícono `bi-moon-stars`/`bi-sun`). `.badge-status-*` son opt-in (no sobre-escriben `bg-*` de Bootstrap). El sidebar es oscuro en ambos temas. Ver Sección 7. |
| **`NavMenu` perdió el seccionado por rol** | El `<AuthorizeView Roles="Administrador">` que envolvía la sección `Gestión` **fue eliminado** (el header es hoy un `<div>` literal): `Clientes y Vehículos`, `Categorías y Servicios` y `Mecánicos` los ve cualquier usuario autenticado, incluido `Mecanico`. La defensa restante son los atributos `[Authorize(Roles = ...)]` de cada página (un mecánico que navegue manualmente a `/clientes` ve la lista en modo lectura; a `/categorias` o `/mecanicos` le sale "No tienes permisos"). Si el ocultamiento del menú era intencional, restaurar el `AuthorizeView` (no necesita `Context="authCtx"`: la trampa RZ9999 solo aplica anidado dentro de `EditForm`). |
| **Identificación del cliente oculta al Mecánico (vista + búsqueda)** | Por privacidad, cédula + tipo del cliente se renderizan tras `<AuthorizeView Roles="Administrador">` en `Clientes.razor` (header y celda de la columna "Identificación") y en la tarjeta de datos de `ClienteDetalle.razor`; para un `Mecanico` esa rama no se renderiza, así que el valor no viaja al cliente (Blazor Server). Además, `Clientes.razor.cs` resuelve el rol en `OnInitializedAsync` (`FindFirst(ClaimTypes.Role) == Roles.Administrador`) y pasa `incluirIdentificacion=false` a `BuscarClientesPaginadoAsync`/`ContarBusquedaAsync`; `ClienteService.AplicarFiltro` omite las cláusulas `CedulaIdentidad` para ese rol (busca solo nombre y placa) y el placeholder cambia a "Buscar por nombre o placa...". Así un Mecánico no puede confirmar una cédula adivinada por la búsqueda. Los modales admin-only (crear/editar/eliminar/transferir) siguen mostrando la cédula y están fuera del alcance del mecánico. |
| **Panel expandible de cliente (vehículos + citas)** | En `/clientes`, cada fila abre un panel embebido bajo la fila con los **vehículos** y las **citas** del cliente. Clic en cualquier parte de la fila lo alterna; el botón chevron de la primera columna es el control accesible (`aria-expanded`/`aria-controls`, header con `visually-hidden`). La **placa** se destaca como badge monoespaciado, tanto en la lista de vehículos como en cada cita (`VehiculoPlaca`). Es un **acordeón**: una sola fila abierta (`filaExpandidaId`), **carga diferida** al abrir y **caché por cliente** (`vehiculosPorCliente`/`citasPorCliente`) para no repetir consultas; se invalida tras desactivar/reactivar/eliminar (los cambios de estado en cascada afectan los vehículos) y se colapsa en cada `CargarAsync` (búsqueda/filtro/página/mutación). Los controles de la fila (link Ver + botones admin) llevan `@onclick:stopPropagation` para no alternar el panel. Carga con `VehiculoService.ObtenerPorClienteAsync(id, incluirInactivos: !cliente.Activo)` + `CitaService.ObtenerPorClienteAsync(id)` (mismo criterio que `ClienteDetalle`). CSS en `Clientes.razor.css` (animación con respeto a `prefers-reduced-motion`). |
| **Advertencia de cliente inactivo al agendar/ver citas** | Los clientes inactivos siguen apareciendo en la búsqueda de `/agendar` y se pueden agendar: `ClienteService.BuscarClientesAsync` (legado) no filtra por `Activo` y `CitaService.AgendarCitaAsync` no valida `Cliente.Activo`. Decisión: **no** ocultarlos ni bloquearlos, sino advertir visualmente. En `/agendar`: fila de resultados gris + badge `Inactivo`, tarjeta de selección en tonos warning, `alert-warning` en el resumen del modal, y chip con ícono de advertencia + tooltip "Cliente inactivo" (datos desde `Cita.Cliente.Activo`, ya incluido por `ObtenerPorRangoAsync`). En `/citas`: badge `Inactivo` junto al nombre. En `/citas/{id}`: tarjeta de cliente con `border-warning` + badge + aviso. Requirió agregar `ClienteActivo` a `CitaDTO`/`CitaDetalleDTO` y a sus proyecciones (`CitaService.ProyeccionListado`, `ObtenerDetallePorIdAsync`). **Caveat de roles**: `/agendar`, `/citas` y `/citas/{id}` son admin-only, así que un `Mecanico` no ve estas advertencias; su único punto de contacto con el estado del cliente sigue siendo el badge de `/clientes` y `/clientes/{id}`. |
| **Mecánico inactivo visible con badge + reasignación de sus citas** | A diferencia del cliente, el mecánico inactivo **no** se ofrece para agendar (`ObtenerTodosAsync(FiltroActivo.Activos)` en la lista del modal, `CitaService.AgendarCitaAsync`/`ReprogramarCitaAsync` lanzan `InactiveMechanicException`). Pero sus citas no canceladas deben seguir visibles y reasignables: el tablero de `/agendar` carga `FiltroActivo.Todos` y conserva los inactivos solo si tienen citas en la semana visible (`Activo || citasSemana.SelectMany(c => c.CitaMecanicos.Select(cm => cm.MecanicoId)).Contains(Id)`), los pinta atenuados con badge `Inactivo` y bloquea el agendado en sus celdas (`PuedeAgendar(dia) && m.Activo` + guard en `AbrirModal`). En `/citas/{id}` la tarjeta lista los mecánicos y marca el badge en cada inactivo (`CitaDetalleDTO.Mecanicos.Any(m => !m.Activo)`) y `AbrirReprogramar` carga estrictamente activos; si el actual está inactivo parte sin ningún activo preseleccionado y muestra un `alert-warning` que obliga a elegir uno activo (el `[MinLength(1)]` del DTO lo revalida). Verificado con Playwright (18/10/2026): desactivar un mecánico con cita → aparece en el tablero con badge y su chip, 0 celdas clicables; reasignar su cita a un activo desde el detalle → la cita cambia de fila en el tablero y el inactivo desaparece de esa semana (ya no tiene citas). |
| **Varios mecánicos por cita (N:M) con `CitaMecanico`** | Una cita puede tener **dos o más mecánicos de la misma especialidad/servicio** (p. ej. un trabajo que requiere dos técnicos). Se introduce la entidad de unión `CitaMecanico` (PK compuesta `CitaId`+`MecanicoId`, FK cita `Cascade` / mecánico `Restrict`) y se **elimina** `Cita.MecanicoId` (relación 1:N anterior). Los write models pasan de `int MecanicoId` a `List<int> MecanicosIds` con `[MinLength(1)]` (`AgendarCitaDTO`, `ReprogramarCitaDTO`); los read models (`CitaDTO`, `CitaDetalleDTO`) exponen `List<MecanicoDTO> Mecanicos` y las tablas usan la propiedad calculada `MecanicosNombres` (separados por coma). Semánticas clave: (1) **solo misma especialidad** —la UI agrupa los mecánicos activos por especialidad y deshabilita los grupos de otras especialidades en cuanto hay una selección, y el servicio se filtra por la especialidad elegida; (2) **el tramo se ocupa para TODOS** —si cualquiera de los asignados tiene solape, se rechaza la cita completa y el `AppointmentOverlapException` nombra al mecánico en conflicto; (3) el tablero pinta el chip de la cita en la fila de **cada** mecánico asignado (`CitasDe` por `CitaMecanicos.Any`), `MecanicoService.ObtenerDisponiblesAsync` y el filtro del listado usan `CitaMecanicos.Any`/`SelectMany`; (4) la reprogramación **reemplaza** el conjunto de mecánicos (`cita.CitaMecanicos.Clear()` + re-add). **Migración**: `AgregarSoporteMultiMecanicoCita` (reordenada a mano para crear la tabla y **backfillear** `INSERT INTO CitaMecanico SELECT CitaId, MecanicoId FROM Cita` *antes* de dropear la columna). **Nota de esquema**: este proyecto crea la BD con `EnsureCreatedAsync` (no `Migrate()`), por lo que no existe `__EFMigrationsHistory`; la migración se generó para versionar el cambio y su DDL se aplicó manualmente con `mysql.exe` sobre la BD compartida preservando los datos. **Verificado con Playwright (18/10/2026)**: (a) el modal de `/agendar` agrupa los mecánicos por especialidad y deshabilita los grupos de otras al elegir uno; (b) agendar con dos mecánicos de la misma especialidad (María López + Pedro Jiménez, Motor; Pedro reactivado para la prueba) crea la cita, pinta el chip en la fila de **ambos** y las tablas la muestran como "María López, Pedro Jiménez"; (c) el filtro de `/citas` por mecánico (`CitaMecanicos.Any`) devuelve solo las citas de ese mecánico; (d) `/citas/{id}` lista todos los asignados y marca el badge `Inactivo`; (e) el solape se valida **por cada** mecánico elegido (marcar a un mecánico ocupado a la 1:00 p.m. deshabilita 12:30/1:00/1:30 en el select de hora); (f) el modal de reprogramación recarga los horarios ocupados al cambiar la fecha y avisa si hay inactivos. **Bug detectado y corregido** en esta misma verificación: `AgendarCitaAsync`/`ReprogramarCitaAsync` **no** validaban la especialidad compartida y el modal de reprogramación listaba todos los mecánicos en plano (sin agrupar ni deshabilitar), así que se podía reprogramar una cita a especialidades mixtas (reproducido: cita Motor reprogramada a María López + Ana Mora de Mantenimiento General y aceptada). Corrección: (1) guard server-side en ambos métodos (`Values.Select(m => m.EspecialidadId).Distinct().Count() > 1` → `InvalidOperationException("Todos los mecánicos de una cita deben pertenecer a la misma especialidad.")`); (2) el modal de reprogramación agrupa por especialidad y deshabilita los grupos ajenos, con la misma regla que `/agendar`; (3) se corrigió el espacio faltante antes del guion largo en la etiqueta "— misma especialidad requerida". |
| **CSS aislado de Blazor no alcanza lo que renderizan los componentes (`Login`)** | El CSS aislado (`*.razor.css`) solo estampa el atributo de scope (`b-<hash>`) en el HTML escrito en el propio `.razor`, **no** en el markup que generan los componentes de Razor. Por eso `InputText`, que renderiza su `<input>`, quedaba sin el atributo y una regla `input[b-<hash>]::placeholder` no matcheaba nunca. Solución aplicada en `Login.razor.css`: `::deep`, que deja la regla sin scope para que sí aplique a los hijos del componente. Es la trampa a recordar si en el futuro falla un estilo en `AgendarCita`, `Citas`, `Clientes` o `NavMenu`. |
| **Placeholder del login muy tenue a propósito (`opacity: .2 !important`)** | `Login.razor.css` fuerza `opacity: .2` sobre el color del token `--placeholder-color` (que ya es un tono apagado: `#4a4e69` light / `#e5e5e5` dark). El `!important` es necesario para vencer el `opacity` por defecto de Bootstrap en `::placeholder`. El efecto combinado es un placeholder casi invisible; si en algún momento se quiere más legibilidad, basta subir el `opacity` o quitarlo (el color del token ya aporta el contraste). |
| **Clases `input-email` / `input-password` sin reglas asociadas** | Se añadieron a los `<InputText>` del login durante el intento de estilar el placeholder, pero el selector final (`::deep input::placeholder`) no las referencia. No rompen nada (el navegador ignora clases sin CSS) y sirven de gancho si se quiere diferenciar por campo; si se prefiere KISS estricto, se pueden quitar de `Login.razor` sin impacto visual. |
| **Contraseña del login sin `type="password"` (equipos compartidos del taller)** | El campo de contraseña de `Login.razor` es siempre `type="text"` con `autocomplete="off"` (nunca `type="password"`), para que el navegador no ofrezca guardar la contraseña en las computadoras compartidas del taller. El ocultado se hace con `-webkit-text-security`, alternando `disc` (oculto) y `none` (visible) según `mostrarPassword` de `Login.razor.cs`; un botón de ojo dentro de un `input-group` alterna el icono `bi-eye-fill`/`bi-eye-slash-fill` y llama `ToggleMostrarPassword`; el `input-group` es de Bootstrap y **no** debe llevar `width: 100%` inline en el `<InputText>` (rompe el `flex-wrap` del grupo y el botón salta a la línea de abajo), así que el estilo inline solo lleva el `-webkit-text-security`. **Ojo con el valor**: es `disc` (como `list-style-type`), **no** `disk`; `disk` es un valor inválido que Chrome ignora, y como el campo es `type="text"` la contraseña quedaría a la vista. Verificado con Playwright (Chrome 155): `type="text"`, `autocomplete="off"`, el computed de `-webkit-text-security` alterna `disc`/`none`, el valor se conserva al alternar y el login sigue autenticando (cookie `.AspNetCore.Cookies`, navega a `/`). **Caveat**: `-webkit-text-security` solo existe en motores WebKit/Blink (Chrome/Edge/Safari); en Firefox el campo se vería en claro (el taller usa Chrome/Edge). |
| **Los `chips` del tablero no se pueden guardar en client-side** | Los chips de cita del `/agendar` se renderizan desde la entidad `Cita` (con `Cliente` incluido por `CitaService.ObtenerPorRangoAsync`), no desde un DTO. La advertencia de cliente inactivo se resuelve con `c.Cliente?.Activo` directamente en el `.razor`; a diferencia de `/citas` y `/citas/{id}`, aquí **no** hizo falta agregar `ClienteActivo` a ningún DTO. |
| **El 401 del login se convertía en un 400 engañoso** | `POST /api/auth/login` devolvía `Results.Unauthorized()` (401), pero `UseStatusCodePagesWithReExecute("/not-found")` re-ejecutaba ese 401 **preservando POST + `application/json`** contra `/not-found`, un endpoint de componente Blazor. `RazorComponentEndpointInvoker` llama a `ReadFormAsync()` en todo POST ([dotnet/aspnetcore#55582](https://github.com/dotnet/aspnetcore/issues/55582)) y falla con un cuerpo no-form → el cliente veía **400 "The request has an incorrect Content-type."** en vez del 401. La UI no lo notaba porque solo mira `response.ok`, pero confundía a clientes de API/logs. Fix: envolver `UseStatusCodePagesWithReExecute` en `app.UseWhen(... !Path.StartsWithSegments("/api"))`, de modo que `/api/*` conserva su código real. |
| **`ReturnUrl` se ignoraba (el login siempre iba a `/`)** | `Login.razor.cs` tenía `NavigateTo("/", forceLoad: true)` hardcodeado, así que entrar desde un deep link (p. ej. `/clientes`) te dejaba en Home tras autenticarte. Fix: `[SupplyParameterFromQuery(Name="ReturnUrl")]` + `EsRutaLocal(url)` (acepta solo `/...` y rechaza `//` y `/\`, para evitar open redirects; si no es válida, usa `/`). Verificado con Playwright: deep link → vuelve a la página pedida; `https://evil.com` y `//evil.com` → caen a `/`. |
| **`@bind-Value:event="oninput"` NO sirve en componentes `InputBase`** | El modificador `:event` aplica a **elementos HTML**, no a componentes. En un componente, `@bind-Value:event="oninput"` renombra el callback de binding y lo tipa como el valor enlazado (`int`), no como `ChangeEventArgs`. Como `InputNumber` vuelca sus `AdditionalAttributes` sobre el `<input>` real, ese `oninput` recibe un `ChangeEventArgs` del DOM y el renderer lo invoca contra un `EventCallback<int>` → `ArgumentException: Object of type 'Microsoft.AspNetCore.Components.ChangeEventArgs' cannot be converted to type 'System.Int32'`, que **rompe el circuito** (la página deja de responder; la UI ni se entera porque el `input` ya tiene el texto). La forma correcta de actualizar en vivo es un handler explícito `@oninput="OnDuracionInput"` con firma `void OnDuracionInput(ChangeEventArgs e)` que parsea `e.Value` y asigna el modelo (ver `CategoriaDetalle.razor.cs`). Detectado al verificar el hint de duración con Playwright (18/10/2026). |
| **Advertencia de cliente inactivo: ¿por qué no se agrega un guard en el servicio?** | `CitaService.AgendarCitaAsync` valida que el mecánico esté activo, el vehículo exista, el servicio exista, la fecha/hora sea válida y no haya solape, pero **no** valida `Cliente.Activo`. Es intencional (decisión de producto): el cliente inactivo se puede agendar y lo que se agrega es una señal visual en las 3 superficies. Si algún día se quiere bloquear el agendar, el guard natural sería en `AgendarCitaAsync` lanzando `InvalidOperationException` (y opcionalmente quitando las advertencias de la UI). |
| **Clase `.content` eliminada del canvas** | El canvas pasó de `article.content px-4` (padding-top 1.1rem) a `article.px-4.py-3`; la regla `.content` ya no existe en `app.css`. Ritmo vertical de todas las páginas cambió levemente (aprobado, Q3-a). |
| **`[EmailAddress]` rechaza la cadena vacía (bloqueaba borrar el correo de notificaciones)** | Detectado con Playwright al verificar `/perfil`: al vaciar `CorreoNotificaciones` no se disparaba ningún submit (la validación mostraba "El correo no tiene un formato válido"). Causa: `EmailAddressAttribute.IsValid("")` devuelve `false`, y Blazor entrega al modelo la cadena vacía (el `<input type="email">` además recorta el valor). Como el campo es **opcional** (vacío = borrar), se quitó `[EmailAddress]` de `ActualizarPerfilAdminDTO` y se dejó solo `[RegularExpression(Patrones.Correo, ...)]`, que trata null/vacío como válido y es **más estricto** que `EmailAddress` (exige punto + extensión alfabética ≥2). Verificado en runtime: `UPDATE Administradors SET CorreoNotificaciones = NULL` + recarga en `''`. **Ojo**: otros DTOs con correo **opcional** (p. ej. `ActualizarClienteDTO.Correo`) tienen la misma combinación `[EmailAddress]` + opcional; si algún día se necesita borrar ese campo, hay que quitar allí el atributo también. |
| **`[RegularExpression]` exige coincidencia completa: un patrón de solo lookaheads nunca valida** | `RegularExpressionAttribute` **ancla** el patrón (equivale a `^(?:patrón)$`), así que `@"(?=.*[a-z])(?=.*[A-Z])(?=.*\d)"` no puede coincidir con ninguna cadena no vacía: los lookaheads son de ancho cero y el ancla final exige consumir el resto del texto. Consecuencia: `CambiarPasswordDTO.NuevaPassword` rechazaba **todas** las contraseñas válidas ("La contraseña debe contener mayúsculas, minúsculas y números") y el cambio de contraseña era imposible; se detectó con Playwright en `/perfil`. Fix: patrón `@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$"` (el `.+` consume los caracteres, así que valida tanto si el atributo ancla como si no). Comprobado con un proyecto de prueba contra `DataAnnotations` real: `Regex.Match(...).Success` crudo es `true`, pero el atributo fallaba; con `.+$` valida. **`RegistroAdminDTO` conserva el patrón roto**, pero es **código muerto** (ninguna página ni servicio lo referencia; `AuthServices.RegisterAdminAsync` no valida complejidad). Si algún día se le da UI al registro de administradores, hay que copiar el patrón corregido. |
| **Aviso de servicio persistente al reintentar con datos inválidos (`/perfil`)** | Si un guardado falla por una regla de negocio (p. ej. "La contraseña actual no es correcta.") y luego se reintenta con datos que no pasan `DataAnnotations`, la alerta de error anterior **sigue visible** junto a los nuevos errores de campo, porque `OnValidSubmit` solo limpia el mensaje al enviar un formulario válido. Es el mismo comportamiento que ya tienen los demás formularios de la app (Clientes, Mecánicos) y se dejó **sin cambios** por consistencia y alcance; si se quiere corregir, basta un `OnInvalidSubmit` que limpie los mensajes de servicio. |
| **`<PageTitle>` solo aplica en carga completa de página** | Limitación preexistente de la app, no del perfil: `App.razor` usa un `<HeadOutlet />` estático, así que al navegar **client-side** el `<title>` no se actualiza (queda "Mecano"); con recarga completa sí aparece "Mi Perfil - Mecano". Se reprodujo también en `/mecanicos`. Sin corregir (requeriría render interactivo del `HeadOutlet`). |
| **Guardado de perfil sin recarga completa (`/perfil`)** | `GuardarPerfil` **no** llama a `CargarPerfil()` (que pone `cargando = true` y desmontaría ambas tarjetas con un parpadeo de spinner): sincroniza en memoria `perfil` y `perfilModel` con los valores normalizados (`Nombre.Trim()`, `Cedula` vía `Formatos.NormalizarIdentificacion`, `CorreoNotificaciones` `Trim`/`null`) que el servicio persiste. La normalización se repite en la página con el mismo helper compartido, así que no puede divergir (el servicio ya la validó y no lanza). Verificado en el log de EF: tras el `UPDATE` **no** hay una nueva proyección de `ObtenerPerfilAsync`. |
| **`Administrador.Cedula` es opcional y se normaliza por tipo al guardarla desde `/perfil`** | La identificación puede quedar vacía (igual que en mecánicos) y se guarda como `null`. Si se provee, `Formatos.NormalizarIdentificacion` la valida y normaliza según `TipoIdentificacion` (Nacional 9 dígitos, DIMEX 11-12, Pasaporte hasta 20) y lanza `InvalidOperationException` con el mensaje concreto, que la página muestra como alerta; nada se persiste si falla. No hay índice único de identificación en `Administrador` (solo `Cliente` lo tiene), así que **no** se validan duplicados de cédula entre administradores, en espejo de `MecanicoService`. Verificado con Playwright: "1-0111-0111" se persiste como `101110111` y el encabezado lo reformatea como "1-0111-0111". |
| **`Administrador.CorreoNotificaciones` es solo notificaciones** | No participa del login: la clave de autenticación sigue siendo `Administrador.Email`, por eso el campo se muestra en solo lectura en `/perfil` (cambiarlo invalidaría el login). Tampoco hay envío real de correos conectado a esta columna; `NotificationService.NotificarCitaAgendadaAsync` hoy solo registra en `NotificacionLog`. El correo secundario queda persistido y validado para un uso futuro. |
| **Cambiar la contraseña no invalida la sesión** | `CambiarPasswordAsync` rehashea `HashPassword` pero no revoca la cookie de autenticación: la sesión en curso sigue válida (decisión de diseño). Verificado con Playwright: tras el cambio, cerrar sesión e iniciar de nuevo funciona con la contraseña nueva, y la anterior deja de servir. |
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
| `Mecano/Components/Pages/Login.razor.css` | Sección 2, 7 y 11 (placeholder vía `::deep` + token `--placeholder-color`) |
| `Mecano/Components/Pages/Clientes/Clientes.razor.css` | Sección 2 y 11 (animación del panel expandible) |
| `Mecano/Components/Layout/ReconnectModal.razor.css` | Sección 2 (plantilla, sin cambios) |
| `Mecano/Components/Pages/AccesoDenegado.razor` | Sección 7 |
| `Mecano/Components/Shared/RedirectToLogin.razor` | Sección 7 |
| `Mecano/Components/Shared/VehiculoFormModal.razor` + `.razor.cs` | Sección 7 |
| `Mecano/Components/Shared/ThemeState.cs` | Sección 7 (tema compartido para los gráficos) |
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
| `Mecano/Data/Migrations/20261011040945_TipoIdentificacion-mecanico-adminitrador.cs` | Sección 5 (agrega `TipoIdentificacion` DEFAULT 0 a `Mecanico` + `Administradors`) |
| `Mecano/Data/Migrations/20261011150853_AgregarTipoIdentificacionMecanico.cs` | Sección 5 y 11 (re-agrega la columna a `Mecanico` con DEFAULT 1; historial duplicado) |
| `Mecano/Components/Pages/Clientes/Clientes.razor(.cs)` | Sección 7 |
| `Mecano/Components/Pages/Clientes/ClienteDetalle.razor(.cs)` | Sección 7 |
| `Mecano/Components/Pages/Mecanicos/Mecanicos.razor(.cs)` | Sección 7 |
| `Mecano/Entidad/DTOs/Mecanico/` (3 DTOs) | Sección 2 y 6 |
| `Mecano/Entidad/DTOs/Dashboard/` (8 DTOs) | Sección 2, 6 y 7 (`/`) |
| `Mecano/Logica/Interfaces/IDashboardService.cs` · `Mecano/Logica/Servicios/DashboardService.cs` | Sección 6 |
| `Mecano/Entidad/Utilidades/HorarioAtencion.cs` | Sección 6 |
| `Mecano/Components/Pages/Home.razor(.cs)` | Sección 7 (`/`, panel de control) |
| `Mecano/Logica/Utilidades/PasswordHasher.cs` | Sección 6 |
| `Mecano/Entidad/DTOs/CitaDTO.cs` | Sección 2 y 6 |
| `Mecano/Entidad/DTOs/CitaDetalleDTO.cs` · `FiltroCitasDTO.cs` · `EditarCitaDTO.cs` | Sección 2 y 6 |
| `Mecano/Components/Pages/Citas/Citas.razor(.cs)` | Sección 7 (`/citas`) |
| `Mecano/Components/Pages/Citas/CitaDetalle.razor(.cs)` | Sección 7 (`/citas/{id}`) |
| `Mecano/Entidad/Utilidades/Formatos.cs` | Sección 5.1 |
| `Mecano/Entidad/Utilidades/Patrones.cs` | Sección 5.1 (regex + mensajes de DataAnnotations compartidos por clientes y mecánicos) |
| `Mecano/Entidad/DTOs/NuevoVehiculoDTO.cs` · `ActualizarVehiculoDTO.cs` | Sección 2 y 6 |
| `Mecano/Logica/Interfaces/IVehiculoService.cs` · `VehiculoService.cs` | Sección 6 |
| `Mecano/Data/Migrations/20261006160916_Vehiculo-Activo-bool.cs` | Sección 5 |
| `Mecano/Entidad/Clases/CitaMecanico.cs` · `Data/Migrations/20261018204110_AgregarSoporteMultiMecanicoCita.cs` | Sección 4 (N:M Cita↔Mecanico) y 11 (decisión y nota de `EnsureCreatedAsync`) |
| `Mecano/wwwroot/app.css` | Sección 7 (tema Industrial Clean) |
| `Mecano/wwwroot/js/theme.js` | Sección 7 |
| `Mecano/Components/Layout/MainLayout.razor(.css)` | Sección 7 |
| `Mecano/Components/Layout/NavMenu.razor(.css)` | Sección 7 |
| `Mecano/Components/Pages/AgendarCita.razor` | Secciones 2, 6 (reglas de agenda), 7 (`/agendar`) y 11 |
| `Mecano/Components/Pages/AgendarCita.razor.css` | Secciones 2 y 7 (estilos del tablero) |
| `Mecano/Logica/Servicios/CitaService.cs` | Sección 6 (guards de `AgendarCitaAsync`) |
| `Mecano/Entidad/DTOs/Perfil/` (3 DTOs) | Sección 2, 6 y 11 (perfil, correo opcional sin `[EmailAddress]`, regex de complejidad con `.+$`) |
| `Mecano/Logica/Interfaces/IAdministradorService.cs` · `Mecano/Logica/Servicios/AdministradorService.cs` | Sección 6 (perfil + cambio de contraseña) |
| `Mecano/Components/Pages/Perfil.razor` + `.razor.cs` + `.razor.css` | Sección 7 y 11 (`/perfil`) |
| `Mecano/Data/Migrations/20261019180239_AgregarCorreoNotificacionesAdmin.cs` | Sección 5 (columna `Administrador.CorreoNotificaciones`) |

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

10. **¿Existe alguna política de contraseña compleja más allá de los 6 caracteres minimum?** — Parcialmente resuelto: `CambiarPasswordDTO` (`/perfil`) exige mínimo 6 caracteres y mayúscula+minúscula+dígito (`[RegularExpression]` corregido, ver Sección 11), y `CambiarPasswordAsync` rechaza repetir la actual; `RegistroAdminDTO` declara la misma regla pero con el patrón roto y **no tiene UI** (código muerto), y `AuthServices.RegisterAdminAsync` sigue sin validar fortaleza. No hay política centralizada: la regla de complejidad vive solo en los DTOs.

11. **¿Cómo se persiste el estado `AuthenticatedUser` entre requests?** — Resuelto: ya no se usa el record en memoria para auth; la sesión vive en la cookie y el estado Blazor se reconstruye vía `CustomAuthenticationStateProvider` (claims del `ClaimsPrincipal`).

12. **CRUD de mecánicos** — Resuelto y extendido: backend (DTOs en `Entidad/DTOs/Mecanico/` con `TipoIdentificacion`, `IMecanicoService`/`MecanicoService` con normalización per-tipo, hashing en `Logica/Utilidades/PasswordHasher.cs` delegado desde `AuthServices`) + UI `Components/Pages/Mecanicos/Mecanicos.razor(.cs)` con selector de tipo y enlace en `NavMenu`. Pendiente: smoke test manual con los 3 tipos y decidir el comportamiento de `DesactivarAsync` frente a citas futuras (ver Sección 11).

13. **Tipos de identificación (Nacional/DIMEX/Pasaporte)** — Resuelto en commit `8ec693b`: `TipoIdentificacion` en `Cliente`, `Mecanico` y `Administrador`; `Formatos.NormalizarIdentificacion`/`FormatearIdentificacion`; UI con `InputSelect` de 3 opciones en Clientes/Detalle/Mecánicos; `RegisterAdminAsync` con overload de cédula+tipo. Pendiente menor: 5 sitios con `CedulaNacional` hardcodeado, comentarios "Fase 1" obsoletos, e historial de migraciones duplicado (ver Sección 11).

14. **CRUD de citas (por fases)** — Plan acordado: **Fase 1** listado + detalle; **Fase 2** edición (datos no-agenda: vehículo, servicio, notas); **Fase 3** reprogramación (fecha/hora/mecánico); **Fase 4** transiciones de estado (flujo lineal `Pendiente → Confirmado → EnProceso → Finalizado`, cancelable en cualquier punto); **Fase 5** cancelar desde la UI; **Fase 6** vista del mecánico. Resueltas Fase 1 (`/citas` + `/citas/{id}`, admin-only), **Fase 2** (`EditarCitaDTO` + `CitaService.ActualizarCitaAsync` + modal de edición en `CitaDetalle`, restringido a la especialidad del mecánico y bloqueado si la cita está cancelado/finalizado) y **Fase 3** (`ReprogramarCitaDTO` + `CitaService.ReprogramarCitaAsync` + modal de reprogramación en `CitaDetalle`: fecha, hora y mecánico, con horarios ocupados por mecánico deshabilitados y la propia cita excluida del cálculo de solape). Además, desde el tablero de `/agendar` un clic en el chip de una cita navega a su detalle. La reprogramación y los cambios de estado son operaciones separadas de la edición.

15. **Regulaciones de entrada (cliente + mecánico)** — Resuelto: reglas de nombre/correo con `[RegularExpression]` vía `Entidad/Utilidades/Patrones.cs` (constantes regex + mensajes) referenciadas desde `CrearClienteDTO`/`ActualizarClienteDTO`/`CrearMecanicoDTO`/`ActualizarMecanicoDTO`; teléfono obligatorio con `[Phone]` y normalizado a 8 dígitos en `ClienteService` y `MecanicoService`; `DbSeeder` siembra teléfono en los 5 mecánicos; se eliminó `IClienteService.RegistrarAsync` (código muerto). Enforcement solo por `EditForm` (`DataAnnotations`), sin barrera server-side adicional. Ver Sección 5.1.

16. **Color del placeholder del login** — Resuelto en `406871a`: el problema no era el selector sino que el CSS aislado de Blazor nunca sella el `<input>` que renderiza `<InputText>`; se corrigió con `::deep input::placeholder` + el token `--placeholder-color` en `app.css` (light/dark). Pendiente: confirmar visualmente en ambos temas (hay que **recompilar y reiniciar** y hacer hard-refresh, porque el CSS aislado es un artefacto de build) y decidir si `opacity: .2` es el nivel de tenuidad deseado.

17. **Advertencia de cliente inactivo en agenda y citas** — Resuelto en `c06de79`: los clientes inactivos se pueden seguir agendando (contrato legado) pero se señalan en `/agendar` (fila gris + badge, tarjeta y modal en tonos warning, ícono + tooltip en el chip), en `/citas` (badge) y en `/citas/{id}` (tarjeta warning + aviso). Pendiente de decisión: hoy **solo el Administrador** ve estas señales, porque las tres rutas son admin-only; un `Mecanico` únicamente encuentra el estado del cliente en `/clientes` y `/clientes/{id}`. Si el requisito era avisar también al mecánico, hay que decidir si se abre `/citas` al rol mecánico (Fase 6 del plan de CRUD de citas) o si se replica la señal en las superficies que el mecánico sí ve.

18. **Correcciones del login (401 puro + `ReturnUrl`)** — Resuelto y verificado con Playwright: `POST /api/auth/login` con credenciales inválidas vuelve a responder **401** sin cuerpo (se excluyó `/api/*` de `UseStatusCodePagesWithReExecute` en `Program.cs`), y el login respeta `?ReturnUrl` solo si es una ruta local (guardas contra open redirect en `Login.razor.cs`). La verificación se hizo con una segunda instancia del build nuevo en `https://localhost:7299`; pendiente reiniciar la instancia principal (`https://mecano:7224`, hoy bajo el depurador de VS) para cargar los cambios.

19. **Personalización del perfil del Administrador Global (datos de identidad + correo de notificaciones + cambio de contraseña)** — Resuelto: página `/perfil` (`[Authorize(Roles = "Administrador")]`, InteractiveServer) con `AdministradorService` (`ObtenerPerfilAsync`/`ActualizarPerfilAsync`/`CambiarPasswordAsync`), DTOs en `Entidad/DTOs/Perfil/`, columna `Administrador.CorreoNotificaciones` (migración `20261019180239`, DDL aplicado a mano sobre la BD existente) y enlaces "Mi Perfil" en `NavMenu` y en el dropdown de usuario de `MainLayout`. Son **editables** el nombre, el tipo y el número de identificación (para cuando el puesto pasa a otra persona) y el correo de notificaciones; el **correo de login** queda de solo lectura (es la clave de autenticación) y la contraseña se cambia sin invalidar la sesión. Verificado con Playwright en `http://localhost:5023` (nombre/tipo/identificación editables con validación por tipo —"1-0111-0111" se persiste como `101110111`—, correo opcional que se borra → `NULL`, unicidad case-insensitive, guards de contraseña y login real con la contraseña nueva). Pendiente de decisión: no hay envío de correos conectado a `CorreoNotificaciones`; queda persistido y validado para un uso futuro.

20. **Panel de control: KPIs + gráficos** — Resuelto en dos fases (commit `016a876` y la fase de gráficos): `/` reemplaza la Home de plantilla con KPIs del día (citas por estado en nav-tabs Blazor-puro, ocupación por minutos-mecánico, taller y personal) y fila de gráficos **Blazor-ApexCharts 7.0.0** (donut de distribución por estado con total al centro y leyenda visible; barras horizontales de tiempo asignado por mecánico activo (serie graficada en **minutos enteros**; marcas del eje «45 min / 1 h / 2 h 30 min» y tooltip «1 hora 30 minutos» vía formatters JS espejo de `Formatos.Duracion`, sección `JsMinutosCorto`/`JsMinutosLargo` en Home.razor.cs); toggle Semana/Mes con default Mes; canceladas fuera de la carga pero dentro del donut; colores sincronizados con `Formatos.BadgeEstado` vía `Formatos.ColorEstado`; tema claro/oscuro vía `ThemeState` + `@key` re-crea los charts; enlaces `.enlace-cascada` con relleno en cascada y borde de cristal). **Pendiente — único pase de verificación con Playwright** (headed, ambos temas, credenciales `adminglobal@mecano.cr`): conteos del donut vs BD, detalle «N citas · duración» de la barra, formato h/min de eje y tooltip de la barra («1 h 30 min» / «1 hora 30 minutos», sin decimales crudos), cambio de período, re-render al alternar a dark, contraste de `.enlace-cascada`, y que un Mecánico vea «No tienes permisos» en `/`.