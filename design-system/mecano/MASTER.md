# Design System — Mecano

> **LOGIC:** Before building a specific page, check `design-system/mecano/pages/[page-name].md`.
> If that file exists, its rules **override** this Master file.
> If not, strictly follow the rules below.

---

**Project:** Mecano (gestión de taller mecánico — Blazor Server + Bootstrap 5)
**Fuente de verdad:** los tokens viven en `wwwroot/app.css` (`:root` y `[data-bs-theme="dark"]`).
**Este documento:** describe ese sistema real. Si hay divergencia, gana `wwwroot/app.css`.

---

## 1. Identidad

**Estilo: Industrial Clean.** Superficies planas sobre gris pizarra, un único acento
cian reservado para la acción y lo interactivo, bordes discretos en lugar de sombras,
tipografía del sistema y densidad de datos alta pero escaneable. Es una herramienta de
trabajo diaria para el personal del taller: la claridad y la velocidad de lectura pesan
más que lo decorativo.

### Diales (0–10)

| Dial | Valor | Implicación |
|---|---|---|
| Densidad | 7 | Tablas densas, tarjetas compactas, jerarquía por tipografía y no por aire. |
| Movimiento | 3 | Solo transiciones funcionales (hover, aparición). Nada llamativo. |
| Variación | 4 | Disposición consistente; rompe el patrón solo para señalizar (insignias de estado, estado vacío). |

### Reglas duras

1. **No cargar activos externos** (fuentes, iconos, JS de CDN ya existente): se usa
   Helvetica del sistema y Bootstrap Icons por CSS. El proyecto original de una versión
   previa del buscador de diseño (Glassmorphism, Fira Code, acento verde) fue **descartado**:
   contradice la identidad ya implementada y habría introducido fuentes y dependencias nuevas.
2. **El color comunica estado, no adorno.** Ningún color fuera de los tokens.
3. **Un solo acento por pantalla.** El cian marca lo interactivo; el resto es neutro.
4. **Claro y oscuro son igual de importantes.** Todo componente nuevo debe verse bien en
   ambos temas sin hex propios por pantalla.

---

## 2. Tokens

Todos los componentes consumen **variables CSS**, nunca hex literales. Así el claro/oscuro
se resuelve con un solo cambio de tema (`data-bs-theme="dark"` en `<html>`).

### Superficie y texto

| Token | Claro | Oscuro | Uso |
|---|---|---|---|
| `--bg-app` | `#F8FAFC` | `#0F172A` | Fondo de la aplicación. |
| `--bg-surface` | `#FFFFFF` | `#1E293B` | Tarjetas, modales, tablas. |
| `--bg-sidebar` | `#1E293B` | `#020617` | Navegación lateral. |
| `--bg-header` | `#FFFFFF` | `#1E293B` | Barra superior. |
| `--text-primary` | `#0F172A` | `#F1F5F9` | Texto principal. |
| `--text-secondary` | `#64748B` | `#94A3B8` | Texto secundario, etiquetas, encabezados de tabla. |
| `--border-color` | `#E2E8F0` | `#334155` | Bordes, separadores, esqueletos. |
| `--placeholder-color` | `#4a4e69` | `#e5e5e5` | Placeholders de formulario. |

### Acento

| Token | Claro | Oscuro | Uso |
|---|---|---|---|
| `--primary-accent` | `#0284C7` | `#38BDF8` | Acción primaria, enlaces, foco, orden activo. |
| `--primary-hover` | `#0369A1` | `#7DD3FC` | Estado hover/active del acento. |
| `--nav-active-bg` | `rgba(2,132,199,.18)` | `rgba(56,189,248,.18)` | Fondo de ítem activo de la navegación. |
| `--nav-hover-bg` | `rgba(255,255,255,.08)` | `rgba(255,255,255,.06)` | Hover sobre superficies oscuras. |

### Estado (insignias)

Se aplican con las clases `.badge-status-*`, que ya apuntan a estos pares de tokens.

| Token par | Claro (bg / texto) | Oscuro (bg / texto) | Semántica |
|---|---|---|---|
| `--badge-success-*` | `#DCFCE7` / `#15803D` | `#14532D` / `#86EFAC` | Activo, confirmado, finalizado. |
| `--badge-warning-*` | `#FEF3C7` / `#B45309` | `#78350F` / `#FCD34D` | Pendiente, atención. |
| `--badge-danger-*` | `#FEE2E2` / `#B91C1C` | `#7F1D1D` / `#FCA5A5` | Cancelado, destructivo. |

### Tipografía

- **Familia:** `'Helvetica Neue', Helvetica, Arial, sans-serif` (definida en `app.css`).
- **Escala:** la de Bootstrap 5 (`h1`–`h6`, `.h3`, `.small`, `.fw-semibold`). No se definen escalas propias.
- **Encabezado de página:** `<h1 class="h3">` + bajada `.text-muted`.
- **Encabezado de tabla:** `.74rem`, mayúsculas, `letter-spacing .04em`, `--text-secondary`.
- **Cifras alineadas:** en columnas numéricas y de precio, `font-variant-numeric: tabular-nums`.

### Espaciado, radios y elevación

Se usan las utilidades de Bootstrap (`gap-*`, `mb-*`, `p-*`). Convenciones:

- Radios: tarjetas `.75rem`, iconos KPI `.6rem`, controles (Bootstrap) `.375rem`.
- Separación entre bloques de página: `1rem` (`mb-3`).
- **Sombra:** por defecto `border` + `shadow-sm`. En tema oscuro `.card` pierde la sombra
  (regla en `app.css`) y queda solo el borde — evita halos sucios sobre fondo oscuro.
- **Foco:** anillo doble (`0 0 0 .1rem white, 0 0 0 .25rem #258cfb`) ya definido globalmente.
  Nunca eliminarlo con `outline: none` salvo en encabezados no interactivos.

### Movimiento

- Duración de referencia: `0.12s` (hover) y `0.24s` (aparición). Nada por encima de `0.3s`.
- Toda animación nueva debe anularse bajo `@media (prefers-reduced-motion: reduce)`.

---

## 3. Componentes

### Botones

| Rol | Clase | Nota |
|---|---|---|
| Primario | `.btn .btn-primary` | Una sola acción primaria por bloque. Icono + texto. |
| Secundario | `.btn .btn-outline-secondary` | Acciones de apoyo y botones de icono. |
| Peligro | `.btn .btn-danger` | Destructivo. |
| Filas | `.btn .btn-sm .btn-outline-primary` / `-warning` / `-success` / `-danger` | Acciones en línea dentro de la fila (Editar, Desactivar, Reactivar, Ver). |

Los botones que solo llevan icono **siempre** requieren `aria-label`.

### Insignias (badges)

- `.badge-status-success` — Activo / Confirmado / Finalizado.
- `.badge-status-warning` — Pendiente.
- `.badge-status-danger` — Cancelado.
- `.badge-status-neutral` — Neutro (p. ej. «Inactivo» o un conteo), `--border-color` de fondo.
- `.badge-estado` — ancho fijo (`6.5rem`) para que los estados alineen en columna.

Nunca usar `.bg-success` / `.bg-secondary` de Bootstrap para estado: no responden al tema oscuro.
Excepción vigente: las vistas de citas (`.badge-estado` y el aviso «Servicio inactivo») siguen
usando `bg-secondary` por coherencia con el estado histórico; pendiente de migrar.

### Tarjetas

`.card.shadow-sm` con `.card-header` / `.card-body`. En oscuro solo borde.
El encabezado aloja acciones (p. ej. «Nuevo …») o una pista breve en `.text-muted.small`.

### Vista administrativa de datos (patrón Clientes)

Patrón único para las vistas de administración de datos (`/clientes`, `/mecanicos`,
`/categorias`, `/categorias/{id}`). Es la referencia visual del sistema: cualquier lista
nueva de datos debe copiarla.

1. Encabezado `<h1><i class="bi bi-…"></i> Título</h1>` (sin `.page-head` ni bajada).
2. Alertas de página: `.alert.alert-success` de éxito y `.alert.alert-danger` de error, ambas
   `.alert-dismissible` con `.btn-close` (`aria-label="Cerrar"`).
3. Una única `.card.shadow-sm`:
   - `.card-header` con `.row.g-2.align-items-center`:
     - búsqueda en `.input-group` (icono `.bi-search` en `.input-group-text` + `input.form-control`
       + botón `.btn-outline-secondary` `bi-x-lg` para limpiar),
     - `.form-select.form-select-sm` de **Estado** (`FiltroActivo`: Activos / Inactivos / Todos),
       solo si la entidad tiene un indicador de activo (Clientes, mecánicos, servicios;
       las categorías no lo tienen),
     - acción primaria «Nuevo …» en la última columna (`text-md-end` con `btn.w-100`, o
       `d-grid d-md-flex justify-content-md-end`, para que ocupe todo el ancho en móvil).
       Si el alta depende de datos auxiliares (p. ej. las especialidades de mecánicos), el
       botón se deshabilita en vez de ocultarse, con `title` que explica el motivo.
   - `.card-body` con uno de tres estados: spinner de carga, `alert alert-info` de vacío, o
     `.table-responsive` con la tabla.
   - `.card-footer` opcional con el conteo en `.text-muted.small` (y la paginación numerada
     cuando la lista es paginada, como en `/clientes`). Solo se renderiza cuando terminó la carga.

La búsqueda es **en memoria** (`@oninput` sobre la lista ya cargada) y el filtro de estado
también: no se vuelve a consultar la base de datos. La lista se carga completa una sola vez
por página (en las vistas con id se reutiliza un guard `idCargado`).

### Tabla de datos

`.table.table-striped.table-hover.align-middle.mb-0` dentro de `.table-responsive`.
Sin estilos propios (las antiguas `.tabla-datos` y los encabezados ordenables se retiraron).
Columnas numéricas, de estado y acciones se centran o alinean a la derecha; las filas de un
registro inactivo se atenúan con `table-secondary`.

### Carga y estado vacío

- Cargando → `spinner-border` centrado (`role="status"` + `.visually-hidden`), nunca la tabla.
- Sin datos → `alert alert-info mb-0`. Distinguir «aún no hay registros» de «la búsqueda no
  encontró nada» mediante un texto contextual (`TextoVacio()`).
- Nunca dejar la pantalla en blanco.

### Acciones por fila (en línea)

Se muestran como botones `.btn.btn-sm` dentro de la última celda (`text-end`), no en un menú `⋮`:
`Ver` (`btn-outline-primary`, enlace), `Editar` (`btn-outline-primary`), `Eliminar`
(`btn-outline-danger`) y, para registros con estado, `Desactivar` (`btn-outline-warning`,
`bi-pause-circle`) o `Reactivar` (`btn-outline-success`, `bi-play-circle`). Los botones que
solo llevan icono requieren `title` y `aria-label`.

Los cambios de estado pasan siempre por un **modal de confirmación** (desactivar y reactivar).
El de desactivar advierte además de que los registros ya existentes se conservan.

### Formularios y modales

- `EditForm` + `DataAnnotationsValidator`; la validación vive en los DTO (DataAnnotations),
  no en la UI.
- Modal: `.modal.fade.show.d-block` con fondo `rgba(0,0,0,.5)` (no hay JS de Bootstrap).
- `InputNumber` engancha `onchange`; si se necesita reaccionar por tecla, añadir un handler
  `@oninput` aparte (ver la duración del servicio).
- **No añadir atributos de validación nativos** (`min`, `max`, `step`, `required`) a los
  `Input*`: la validación del navegador bloquearía el `submit`, `OnValidSubmit` nunca se
  dispararía y las `ValidationMessage` no aparecerían. El rango se documenta en texto de
  ayuda y lo aplica el DTO.
- Toda entrada lleva `label` (o `.visually-hidden` si el contexto ya la nombra) y,
  si puede fallar, `ValidationMessage` en `.text-danger.small`.

### Alertas

`.alert.alert-success` / `.alert-danger` con `.alert-dismissible` y botón `.btn-close`
(`aria-label="Cerrar"`). Las de éxito se descartan a mano; el error general de página solo
se muestra si no hay un modal abierto que ya lo esté mostrando.

---

## 4. Iconografía

- **Bootstrap Icons** (`bi bi-*`), cargados por CSS. No introducir otras librerías.
- Decorativos: `aria-hidden="true"`. Interactivos sin texto: `aria-label` obligatorio.
- Repertorio usado: `plus-circle`, `pencil`, `trash`, `eye`, `search`, `x-lg`, `arrow-left`,
  `tags`, `tag`, `wrench-adjustable`, `people`, `pause-circle`, `play-circle`, `check-lg`,
  `calendar-check`.

---

## 5. Accesibilidad

Requisitos mínimos para cualquier interfaz nueva:

1. **Contraste 4.5:1** en texto. Los pares de tokens de estado ya lo cumplen en ambos temas.
2. **Nombres accesibles** en todo control interactivo (texto visible o `aria-label`).
3. **Teclado completo**: orden de tabulación lógico y foco visible; ningún control inalcanzable.
4. **`aria-label`** en botones de solo icono (y `title` cuando el icono necesita contexto);
   `role="status"` + `.visually-hidden` en el spinner de carga.
5. **`prefers-reduced-motion`**: anular animaciones no esenciales.
6. **Hint semántico** `Gradients & Linear`: no aplica; el sistema no usa degradados.
7. Los encabezados de columna y los `label` usan asociación real (`scope`, `for`/`id`).

---

## 6. Referencias

- Tokens y utilidades: `wwwroot/app.css` (definiciones de `:root` / `[data-bs-theme="dark"]`,
  `.badge-status-*` y el resto de utilidades compartidas).
- Guía específica de las vistas de categorías y servicios:
  `design-system/mecano/pages/categorias-servicios.md`.
- Estado del proyecto y convenciones de desarrollo: `PROJECT_STATE.md` (raíz del repo).
