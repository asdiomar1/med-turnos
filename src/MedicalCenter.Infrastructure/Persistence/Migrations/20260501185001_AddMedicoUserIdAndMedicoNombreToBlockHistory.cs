using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations
{
    /// <summary>
/// Adds MedicoUserId (Guid, nullable) and MedicoNombre (string, nullable) columns to BlockHistory.
/// These columns support doctor assignment tracking with Guid identity and human-readable names.
/// </summary>
    public partial class AddMedicoUserIdAndMedicoNombreToBlockHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "medico_user_id",
                table: "historial_bloques",
                type: "uuid",
                nullable: true,
                comment: "Doctor/GUID identity for block history assignment");

            migrationBuilder.AddColumn<string>(
                name: "medico_nombre",
                table: "historial_bloques",
                type: "character varying(200)",
                nullable: true,
                comment: "Human-readable doctor name for block history display");

 
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_historial_bloques_medico_user_id",
                table: "historial_bloques");

            migrationBuilder.DropColumn(
                name: "medico_user_id",
                table: "historial_bloques");

            migrationBuilder.DropColumn(
                name: "medico_nombre",
                table: "historial_bloques");
        }
    }
}
