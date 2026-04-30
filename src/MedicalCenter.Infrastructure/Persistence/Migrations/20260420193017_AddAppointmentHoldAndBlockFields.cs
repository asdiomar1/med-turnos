using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentHoldAndBlockFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApartadoPorUserId",
                table: "appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApartadoTs",
                table: "appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TandaId",
                table: "appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_appointments_Fecha_Hora_CameraId",
                table: "appointments",
                columns: new[] { "Fecha", "Hora", "CameraId" });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_TandaId",
                table: "appointments",
                column: "TandaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_appointments_Fecha_Hora_CameraId",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "IX_appointments_TandaId",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "ApartadoPorUserId",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "ApartadoTs",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "TandaId",
                table: "appointments");
        }
    }
}
