using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutOfHoursTurnsCompatibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS public.turnos_fuera_horario (
                    "Id" uuid NOT NULL,
                    "Fecha" date NOT NULL,
                    "Hora" time without time zone NOT NULL,
                    "PacienteId" uuid NOT NULL,
                    "Notas" text NULL,
                    "CreadoPor" uuid NOT NULL,
                    "OperadorCamaraId" uuid NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                    "EsMonoxido" boolean NOT NULL DEFAULT false,
                    "MonoxidoOrdenMedica" boolean NOT NULL DEFAULT false,
                    "MonoxidoResumenClinico" boolean NOT NULL DEFAULT false,
                    "MonoxidoMedicoId" integer NULL,
                    CONSTRAINT "PK_turnos_fuera_horario" PRIMARY KEY ("Id")
                );

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_turnos_fuera_horario_Fecha_Hora_PacienteId"
                    ON public.turnos_fuera_horario ("Fecha", "Hora", "PacienteId");

                CREATE INDEX IF NOT EXISTS "IX_turnos_fuera_horario_PacienteId"
                    ON public.turnos_fuera_horario ("PacienteId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS public.turnos_fuera_horario;
                """);
        }
    }
}
