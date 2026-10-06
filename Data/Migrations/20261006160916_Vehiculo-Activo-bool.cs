using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mecano.Data.Migrations
{
    /// <inheritdoc />
    public partial class VehiculoActivobool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Vehiculo",
                type: "tinyint(1)",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Vehiculo");
        }
    }
}
