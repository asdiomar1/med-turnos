using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations
{
    /// <summary>
/// Adds MedicoNombre (string, nullable) column to Appointment.
/// This column provides human-readable doctor names alongside existing MedicoId/MedicoUserId tracking.
/// </summary>
    public partial class AddMedicoNombreToAppointment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "medico_nombre",
                table: "appointments",
                type: "character varying(200)",
                nullable: true,
                comment: "Human-readable doctor name for appointment display");


        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_appointments_medico_nombre",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "medico_nombre",
                table: "appointments");
        }
    }
}
