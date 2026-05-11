using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicoNombreColumnsToCoreTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IdGuid",
                table: "medicos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "medico_nombre",
                table: "historial_bloques",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "medico_user_id",
                table: "historial_bloques",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "medico_nombre",
                table: "consultas_slots",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "medico_nombre",
                table: "appointments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdGuid",
                table: "medicos");

            migrationBuilder.DropColumn(
                name: "medico_nombre",
                table: "historial_bloques");

            migrationBuilder.DropColumn(
                name: "medico_user_id",
                table: "historial_bloques");

            migrationBuilder.DropColumn(
                name: "medico_nombre",
                table: "consultas_slots");

            migrationBuilder.DropColumn(
                name: "medico_nombre",
                table: "appointments");
        }
    }
}
