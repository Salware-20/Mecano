using System;

namespace Mecano.Entidad.DTOs.Auditoria
{
    /// <summary>
    /// Filtros opcionales para el visor paginado de auditoría (/auditoria).
    /// Todos los campos son opcionales: si están vacíos/nulos se ignoran.
    /// </summary>
    public class FiltroAuditoriaDTO
    {
        /// <summary>Fecha de inicio del rango (inclusive, se compara por día).</summary>
        public DateTime? Desde        { get; set; }

        /// <summary>Fecha de fin del rango (inclusive, se compara por día).</summary>
        public DateTime? Hasta        { get; set; }

        /// <summary>Texto libre para filtrar por TipoAccion (Contains, case-insensitive en BD).</summary>
        public string?   TipoAccion   { get; set; }

        /// <summary>Texto libre para filtrar por UsuarioNombre (Contains, case-insensitive en BD).</summary>
        public string?   UsuarioNombre { get; set; }
    }
}
