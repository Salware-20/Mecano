using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mecano.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTipoIdentificacionCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cliente_CedulaIdentidad",
                table: "Cliente");

            migrationBuilder.AddColumn<int>(
                name: "TipoIdentificacion",
                table: "Cliente",
                type: "int",
                nullable: false,
                // TipoIdentificacion.Nacional = 1. EF no puede deducirlo porque el enum
                // no tiene un miembro con valor 0; sin este default las filas existentes
                // quedan en 0, que no es un valor válido del enum.
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_CedulaIdentidad_TipoIdentificacion",
                table: "Cliente",
                columns: new[] { "CedulaIdentidad", "TipoIdentificacion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cliente_CedulaIdentidad_TipoIdentificacion",
                table: "Cliente");

            migrationBuilder.DropColumn(
                name: "TipoIdentificacion",
                table: "Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_CedulaIdentidad",
                table: "Cliente",
                column: "CedulaIdentidad",
                unique: true);
        }
    }
}
