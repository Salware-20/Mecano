using System.Text;
using Mecano.Entidad.Utilidades;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs;

namespace Mecano.Entidad.Utilidades
{
    /// <summary>
    /// Utilidades para generar enlaces de WhatsApp para Costa Rica (prefijo 506).
    /// Usa únicamente datos ya disponibles en la capa de presentación (teléfono en dígitos).
    /// </summary>
    public static class WhatsAppHelper
    {
        private const string CodigoPaisCR = "506";
        private const int LargoTelefonoCR = 8;
        private const int LargoConPrefijoCR = 11; // 506 + 8 dígitos
        private const int MaxLongitudMensaje = 1000;

        /// <summary>
        /// Resultado de generar un mensaje según el estado de la cita.
        /// </summary>
        public readonly record struct MensajeEstado(
            string Asunto,
            string Cuerpo,
            bool Enviar
        );

        /// <summary>
        /// Normaliza un número telefónico costarricense para usar con wa.me.
        /// Acepta números con o sin prefijo 506. Requiere exactamente 8 dígitos después
        /// de normalizar. Devuelve cadena vacía si es inválido.
        /// </summary>
        public static string NormalizarParaWhatsApp(string? telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
                return string.Empty;

            var digitos = Formatos.SoloDigitos(telefono);
            if (string.IsNullOrEmpty(digitos))
                return string.Empty;

            // Si ya trae prefijo 506 (11 dígitos), validar que termine con 8 dígitos
            if (digitos.Length == LargoConPrefijoCR)
            {
                if (digitos.StartsWith(CodigoPaisCR))
                {
                    var numeroLocal = digitos.Substring(2);
                    if (numeroLocal.Length == LargoTelefonoCR)
                        return digitos;
                }
                return string.Empty;
            }

            // Debe tener exactamente 8 dígitos (número local CR)
            if (digitos.Length == LargoTelefonoCR)
            {
                return $"{CodigoPaisCR}{digitos}";
            }

            return string.Empty;
        }

        /// <summary>
        /// Indica si el teléfono puede usarse con WhatsApp (número CR válido).
        /// </summary>
        public static bool EsValidoParaWhatsApp(string? telefono)
        {
            return !string.IsNullOrEmpty(NormalizarParaWhatsApp(telefono));
        }

        /// <summary>
        /// Crea un enlace wa.me con mensaje pre-llenado. Trunca el mensaje si es muy largo.
        /// Devuelve cadena vacía si el teléfono no es válido para WhatsApp.
        /// </summary>
        public static string CrearEnlace(string? telefono, string? mensaje)
        {
            var numero = NormalizarParaWhatsApp(telefono);
            if (string.IsNullOrEmpty(numero))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(mensaje))
                return $"https://wa.me/{numero}";

            var mensajeTruncado = TruncarMensaje(mensaje);
            var mensajeCodificado = Uri.EscapeDataString(mensajeTruncado);
            return $"https://wa.me/{numero}?text={mensajeCodificado}";
        }

        /// <summary>
        /// Genera el mensaje apropiado según el estado de la cita.
        /// Incluye: servicio, fecha, hora, vehículo, mecánico, precio (admin), total (Finalizado).
        /// </summary>
        public static MensajeEstado CrearMensajeSegunEstado(CitaDetalleDTO cita)
        {
            var sb = new StringBuilder();
            var estado = cita.Estado;

            // Encabezado común
            sb.AppendLine("Estimado/a");
            if (!string.IsNullOrWhiteSpace(cita.ClienteNombre))
                sb.AppendLine(cita.ClienteNombre.Trim());
            sb.AppendLine();

            // Detalles del vehículo
            var vehiculoInfo = string.Empty;
            if (!string.IsNullOrWhiteSpace(cita.VehiculoMarca) || !string.IsNullOrWhiteSpace(cita.VehiculoModelo) || !string.IsNullOrWhiteSpace(cita.VehiculoPlaca))
            {
                vehiculoInfo = $"{cita.VehiculoMarca} {cita.VehiculoModelo}".Trim();
                if (!string.IsNullOrWhiteSpace(cita.VehiculoPlaca))
                    vehiculoInfo += $" ({cita.VehiculoPlaca})";
            }

            // Detalles del mecánico (solo admin)
            var mecanicoInfo = string.Empty;
            if (cita.Mecanicos?.Any() == true)
            {
                var nombres = string.Join(", ", cita.Mecanicos.Select(m => m.Nombre).Where(n => !string.IsNullOrWhiteSpace(n)));
                if (!string.IsNullOrWhiteSpace(nombres))
                    mecanicoInfo = $"\nMecánico asignado: {nombres}";
            }

            // Precio (solo admin)
            var precioInfo = string.Empty;
            if (cita.ServicioPrecio > 0)
            {
                precioInfo = $"\nPrecio del servicio: {Formatos.Moneda(cita.ServicioPrecio)}";
            }

            return estado switch
            {
                EstadoCita.Pendiente => new MensajeEstado(
                    Asunto: $"Confirmación de cita #{cita.Id} - Mecano",
                    Cuerpo: $"{sb}Su cita ha sido registrada y está pendiente de confirmación por la administración.\n\n" +
                            $"Servicio: {cita.ServicioNombre}\n" +
                            $"Fecha: {cita.Fecha:dd/MM/yyyy}\n" +
                            $"Hora: {cita.HoraInicio:hh\\:mm} - {cita.HoraFin:hh\\:mm}\n" +
                            (string.IsNullOrWhiteSpace(vehiculoInfo) ? "" : $"Vehículo: {vehiculoInfo}\n") +
                            $"{mecanicoInfo}\n" +
                            $"{precioInfo}\n" +
                            "\nLe notificaremos cuando la cita sea confirmada. ¡Gracias por confiar en Mecano!",
                    Enviar: true
                ),

                EstadoCita.Confirmado => new MensajeEstado(
                    Asunto: $"Recordatorio de cita #{cita.Id} - Mecano",
                    Cuerpo: $"{sb}Le recordamos que su cita está confirmada para:\n\n" +
                            $"Servicio: {cita.ServicioNombre}\n" +
                            $"Fecha: {cita.Fecha:dd/MM/yyyy}\n" +
                            $"Hora: {cita.HoraInicio:hh\\:mm} - {cita.HoraFin:hh\\:mm}\n" +
                            (string.IsNullOrWhiteSpace(vehiculoInfo) ? "" : $"Vehículo: {vehiculoInfo}\n") +
                            $"{mecanicoInfo}\n" +
                            $"{precioInfo}\n" +
                            "\nPor favor llegue 10 minutos antes de la hora programada. ¡Gracias por confiar en Mecano!",
                    Enviar: true
                ),

                EstadoCita.EnProceso => new MensajeEstado(
                    Asunto: string.Empty,
                    Cuerpo: string.Empty,
                    Enviar: false
                ),

                EstadoCita.Finalizado => new MensajeEstado(
                    Asunto: $"Su vehículo está listo para retiro - Cita #{cita.Id}",
                    Cuerpo: $"{sb}Le informamos que el servicio '{cita.ServicioNombre}' para su vehículo {vehiculoInfo} ha finalizado con éxito y ya se encuentra listo para retiro en nuestras instalaciones.\n\n" +
                            $"Fecha de finalización: {DateTime.Now:dd/MM/yyyy}\n" +
                            $"Total a cancelar: {Formatos.Moneda(cita.ServicioPrecio)}\n\n" +
                            "¡Gracias por su confianza en Mecano!",
                    Enviar: true
                ),

                EstadoCita.Cancelado => new MensajeEstado(
                    Asunto: $"Cita #{cita.Id} cancelada - Mecano",
                    Cuerpo: $"{sb}Le informamos que su cita ha sido cancelada.\n\n" +
                            $"Servicio: {cita.ServicioNombre}\n" +
                            $"Fecha programada: {cita.Fecha:dd/MM/yyyy}\n" +
                            $"Hora: {cita.HoraInicio:hh\\:mm} - {cita.HoraFin:hh\\:mm}\n" +
                            (string.IsNullOrWhiteSpace(vehiculoInfo) ? "" : $"Vehículo: {vehiculoInfo}\n") +
                            "\nSi desea reprogramar, contáctenos. ¡Gracias por confiar en Mecano!",
                    Enviar: true
                ),

                _ => new MensajeEstado(string.Empty, string.Empty, false)
            };
        }

        /// <summary>
        /// Crea un mensaje pre-llenado para contactar sobre una cita (uso admin).
        /// </summary>
        public static string CrearMensajeParaCita(int citaId, string? clienteNombre, string? placa, DateTime fecha, TimeOnly horaInicio)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Hola");
            if (!string.IsNullOrWhiteSpace(clienteNombre))
                sb.AppendLine(clienteNombre.Trim());
            sb.AppendLine();
            sb.AppendLine($"Le escribimos de Mecano sobre su cita #{citaId}.");
            if (!string.IsNullOrWhiteSpace(placa))
                sb.AppendLine($"Vehículo: {placa.Trim()}");
            sb.AppendLine($"Fecha: {fecha:dd/MM/yyyy}");
            sb.AppendLine($"Hora: {horaInicio:hh\\:mm}");
            sb.AppendLine();
            sb.AppendLine("¿Podemos ayudarle en algo?");
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Crea un mensaje genérico para contactar a un cliente (uso admin).
        /// </summary>
        public static string CrearMensajeParaCliente(string? clienteNombre)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Hola");
            if (!string.IsNullOrWhiteSpace(clienteNombre))
                sb.AppendLine(clienteNombre.Trim());
            sb.AppendLine();
            sb.AppendLine("Le escribimos de Mecano. ¿En qué podemos ayudarle?");
            return sb.ToString().TrimEnd();
        }

        private static string TruncarMensaje(string mensaje)
        {
            if (string.IsNullOrEmpty(mensaje))
                return string.Empty;

            if (mensaje.Length <= MaxLongitudMensaje)
                return mensaje;

            return mensaje.Substring(0, MaxLongitudMensaje - 3) + "...";
        }
    }
}