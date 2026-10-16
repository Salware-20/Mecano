using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mecano.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarKilometrajeVehiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kilometraje",
                table: "Vehiculo",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kilometraje",
                table: "Vehiculo");
        }
    }
}
