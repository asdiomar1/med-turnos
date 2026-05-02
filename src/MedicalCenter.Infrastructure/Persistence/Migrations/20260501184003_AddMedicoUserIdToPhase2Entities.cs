using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicoUserIdToPhase2Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "monoxido_medico_user_id",
                table: "turnos_fuera_horario",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "medico_user_id",
                table: "historia_clinica_evoluciones",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "medico_user_id",
                table: "consultas_slots",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "monoxido_medico_user_id",
                table: "turnos_fuera_horario");

            migrationBuilder.DropColumn(
                name: "medico_user_id",
                table: "historia_clinica_evoluciones");

            migrationBuilder.DropColumn(
                name: "medico_user_id",
                table: "consultas_slots");
        }
    }
}
