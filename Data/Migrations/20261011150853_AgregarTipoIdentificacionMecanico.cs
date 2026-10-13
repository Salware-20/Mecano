using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mecano.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTipoIdentificacionMecanico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoIdentificacion",
                table: "Mecanico",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoIdentificacion",
                table: "Mecanico");
        }
    }
}
