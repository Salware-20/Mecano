# Categorías y Servicios

> Reglas específicas de `/categorias` y `/categorias/{id}`. **Overriden** al Master
> (`design-system/mecano/MASTER.md`) cuando haya conflicto.

---

## Contexto

Dos pantallas encadenadas por el flujo natural (categoría → sus servicios):

| Ruta | Archivos | Contenido |
|---|---|---|
| `/categorias` | `Components/Pages/Categoria/Categorias.razor(.cs)` | Lista de categorías. |
| `/categorias/{id}` | `Components/Pages/Categoria/CategoriaDetalle.razor(.cs)` | Servicios de una categoría. |

Ambas siguen la **vista administrativa de datos (patrón Clientes)** descrita en el Master:
encabezado `<h1>`, alertas de página, una única `.card.shadow-sm` con barra de herramientas en
el `.card-header` y tabla Administración debajo.

> **Historial.** Una iteración previa de estas dos pantallas (rejilla de KPI, esqueletos,
> ordenamiento por columnas, pestañas de estado en la URL, menú `⋮` por fila y edición en línea
> por doble clic) fue **descartada**. Su CSS (`.page-head`, `.kpi-*`, `.toolbar`,
> `.search-field`, `.tabla-datos`, `.th-sort-btn`, `.skeleton`, `.fade-in`, `.empty-state`,
> `.menu-acciones`, `.fila-edicion`, `.tabs-estado`) se retiró de `wwwroot/app.css`. No
> reintroducir esos patrones: la referencia es la vista de Clientes.

---

## `/categorias` — lista

1. `<h1><i class="bi bi-tags"></i> Gestión de Categorías</h1>`.
2. Alertas de página (éxito y error), descartables.
3. `.card.shadow-sm`:
   - `.card-header`: búsqueda por **nombre o descripción** (`.input-group`) + botón
     «Nueva categoría» (`.btn-primary`).
   - `.card-body`: spinner, `alert alert-info` o tabla.
4. Tabla Nombre / Descripción / Servicios / Mecánicos / Acciones.
   - Servicios y Mecánicos son conteos: `.badge-status-success` y `.badge-status-neutral`
     respectivamente; si el conteo es `0` se muestra un `0` atenuado.
   - Acciones en línea: `Ver` (enlace a `/categorias/{id}`), `Editar`, `Eliminar`.
5. Modales: crear/editar (Nombre `*`, Descripción) y confirmación de eliminación.

`CategoriaDTO` no tiene campo `Activo`: la lista **no** tiene filtro de Estado ni paginación.
La búsqueda es en memoria (la lista se carga completa una vez).

Estados:

- Sin categorías → `alert alert-info` «Aún no hay categorías registradas…».
- Búsqueda sin coincidencias → `alert alert-info` «No se encontraron categorías…».
- El error del servicio (p. ej. nombre duplicado, categoría con servicios) se muestra dentro
  de su modal mientras está abierto; si no hay modal, como alerta de página.

---

## `/categorias/{id}` — servicios

1. `breadcrumb` + `<h1>` con el nombre de la categoría (o su descripción) + botón «Volver».
2. Alertas de página.
3. `.card.shadow-sm`:
   - `.card-header`: búsqueda por **nombre o descripción** + **Estado**
     (`FiltroActivo`: Activos / Inactivos / Todos) + «Nuevo servicio».
   - `.card-body`: spinner, `alert alert-info` o tabla.
4. Tabla Nombre / Descripción / Duración / Precio / Estado / Acciones.
   - Duración con `Formatos.Duracion`, precio con `Formatos.Moneda` (`₡`).
   - Estado: `.badge-status-success` «Activo» / `.badge-status-neutral` «Inactivo».
   - Las filas inactivas se atenúan con `table-secondary`.
   - Acciones en línea: `Editar` y `Desactivar` (`bi-pause-circle`, `btn-outline-warning`) o
     `Reactivar` (`bi-play-circle`, `btn-outline-success`) según el estado.
5. Modales: crear/editar, confirmación de desactivación y confirmación de reactivación.

Los servicios se cargan **completos una sola vez** (`ObtenerPorCategoriaAsync(id,
soloActivos: false)`) y el filtro de estado + la búsqueda se aplican en memoria. Se recarga
solo cuando cambia el `Id` de la categoría (guard `idCargado`).

### Desactivar / reactivar (contrato funcional)

- **Desactivar** es una baja lógica (`ServicioService.EliminarAsync`) y siempre pide
  confirmación. El modal avisa de que el servicio dejará de ofrecerse al agendar y de que las
  citas ya registradas se conservan.
- **Reactivar** (`ServicioService.ReactivarAsync`) también pide confirmación.
- Un servicio desactivado **no se puede agendar**:
  - la UI de `AgendarCita` solo ofrece servicios activos, y
  - `CitaService.AgendarCitaAsync` lanza `InactiveServiceException` como defensa en
    profundidad (cubre la desactivación ocurrida después de cargar la página). El modal de
    agendar muestra ese mensaje sin cerrarse.
- Al **editar** una cita se conserva el servicio actual aunque esté inactivo; solo se bloquea
  cambiarla a uno inactivo (`ActualizarCitaAsync`).
- Las citas ya registradas **señalizan** el servicio inactivo:
  - `Citas` (lista): insignia «Servicio inactivo» junto al nombre del servicio.
  - `CitaDetalle`: la tarjeta del servicio pasa a `border-warning`, con insignia «Inactivo» y
    una `alert-warning` que explica que no admite citas nuevas.
  - Lo mismo en las vistas de cliente y vehículo (`Clientes`, `ClienteDetalle`,
    `VehiculoDetalle`).
  - Estas insignias usan `bg-secondary` por coherencia con el estado histórico de las vistas de
    citas (ver la excepción en el Master).

### Validación del formulario de servicio

Los `InputNumber` del modal **no** llevan `min`/`max`/`step` nativos. Con ellos, la validación
del navegador bloqueaba el evento `submit` y `OnValidSubmit` no llegaba a dispararse, de modo
que las `ValidationMessage` (p. ej. «La duración debe estar entre 5 y 480 minutos») no
aparecían. El rango se documenta en texto de ayuda y lo aplica `CrearServicioDTO`
(DataAnnotations).

---

## Decisiones y límites

- **Iconos:** solo Bootstrap Icons. No se cargan activos externos.
- **Sin JS de Bootstrap:** los modales usan `.modal.fade.show.d-block` con velo propio.
- **Acciones en lote (selección múltiple):** fuera de alcance.
- **Contadores por servicio** (cuántas citas futuras usan un servicio): fuera de alcance
  (YAGNI); la desactivación no bloquea nada, así que no hay decisión que tomar.
- **Ingeniería:** YAGNI/KISS. Sin paginación, ordenamiento por columnas ni búsqueda en servidor
  mientras el volumen real no lo justifique.

---

## Checklist de verificación

- [ ] Recargar `/categorias` sin caché: `app.css` responde 200.
- [ ] Todas las filas y controles esperados existen (`#selServiciosEstado`, botones `Nuevo …`).
- [ ] Búsqueda y filtro de estado reducen la tabla sin recargar.
- [ ] Desactivar → confirmación → alerta de éxito → el servicio sale de «Activos» y aparece en
      «Inactivos» con insignia «Inactivo» y botón Reactivar.
- [ ] Reactivar → confirmación → alerta de éxito → vuelve a «Activos».
- [ ] Un servicio desactivado no aparece en el selector de `AgendarCita`.
- [ ] Si se desactiva con la página de agendar ya cargada, el backend rechaza el agendado y el
      modal muestra el mensaje.
- [ ] Una cita con servicio desactivado muestra «Servicio inactivo» (lista y detalle).
- [ ] El modal de servicio muestra las `ValidationMessage` al enviar vacío o fuera de rango.
- [ ] Claro y oscuro: contraste correcto en insignias y `table-secondary`.
