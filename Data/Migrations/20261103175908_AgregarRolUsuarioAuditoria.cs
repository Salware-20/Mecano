using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mecano.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRolUsuarioAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsuarioRol",
                table: "AuditoriaLogs",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsuarioRol",
                table: "AuditoriaLogs");
        }
    }
}
