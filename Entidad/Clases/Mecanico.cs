using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.Clases
{
    /// <summary>
    /// Represents a mechanic employed at the shop.
    /// </summary>
    public class Mecanico
    {
        public int MecanicoId { get; set; }

        [MaxLength(20)]
        public string? Cedula { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string HashPassword { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telefono { get; set; }

        public int EspecialidadId { get; set; }
        public Categoria Especialidad { get; set; } = null!;

        public bool Activo { get; set; } = true;

        public ICollection<Cita> Citas { get; } = new List<Cita>();
    }
}
