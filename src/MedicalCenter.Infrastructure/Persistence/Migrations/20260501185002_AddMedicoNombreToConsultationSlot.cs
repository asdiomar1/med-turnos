using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations
{
    /// <summary>
/// Adds MedicoNombre (string, nullable) column to ConsultationSlot.
/// This column provides human-readable doctor names alongside existing MedicoId/MedicoUserId tracking.
/// </summary>
    public partial class AddMedicoNombreToConsultationSlot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "medico_nombre",
                table: "consultas_slots",
                type: "character varying(200)",
                nullable: true,
                comment: "Human-readable doctor name for consultation slot display");


        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_consultas_slots_medico_nombre",
                table: "consultas_slots");

            migrationBuilder.DropColumn(
                name: "medico_nombre",
                table: "consultas_slots");
        }
    }
}
