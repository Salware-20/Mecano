using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mecano.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSoporteMultiMecanicoCita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Se crea la tabla de unión N:M.
            migrationBuilder.CreateTable(
                name: "CitaMecanico",
                columns: table => new
                {
                    CitaId = table.Column<int>(type: "int", nullable: false),
                    MecanicoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitaMecanico", x => new { x.CitaId, x.MecanicoId });
                    table.ForeignKey(
                        name: "FK_CitaMecanico_Cita_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Cita",
                        principalColumn: "CitaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CitaMecanico_Mecanico_MecanicoId",
                        column: x => x.MecanicoId,
                        principalTable: "Mecanico",
                        principalColumn: "MecanicoId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CitaMecanico_MecanicoId",
                table: "CitaMecanico",
                column: "MecanicoId");

            // 2) Se migran los datos: cada cita existente conserva su mecánico actual
            //    como primera (y única) asignación en la nueva tabla.
            migrationBuilder.Sql(
                "INSERT INTO `CitaMecanico` (`CitaId`, `MecanicoId`) SELECT `CitaId`, `MecanicoId` FROM `Cita`;");

            // 3) Recién ahora se elimina la relación 1:N anterior.
            migrationBuilder.DropForeignKey(
                name: "FK_Cita_Mecanico_MecanicoId",
                table: "Cita");

            migrationBuilder.DropIndex(
                name: "IX_Cita_MecanicoId",
                table: "Cita");

            migrationBuilder.DropColumn(
                name: "MecanicoId",
                table: "Cita");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MecanicoId",
                table: "Cita",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Best-effort: se recupera el primer mecánico asignado de cada cita.
            migrationBuilder.Sql(
                "UPDATE `Cita` c SET c.`MecanicoId` = (SELECT cm.`MecanicoId` FROM `CitaMecanico` cm WHERE cm.`CitaId` = c.`CitaId` LIMIT 1);");

            migrationBuilder.CreateIndex(
                name: "IX_Cita_MecanicoId",
                table: "Cita",
                column: "MecanicoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cita_Mecanico_MecanicoId",
                table: "Cita",
                column: "MecanicoId",
                principalTable: "Mecanico",
                principalColumn: "MecanicoId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropTable(
                name: "CitaMecanico");
        }
    }
}
