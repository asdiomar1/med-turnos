using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations;

public partial class AddHistoriasClinicas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.historias_clinicas (
                paciente_id               uuid                     NOT NULL,
                numero                    bigint                   NOT NULL,
                antecedentes              text,
                alergias                  text,
                medicacion_actual         text,
                observaciones_relevantes  text,
                created_at                timestamp with time zone NOT NULL,
                updated_at                timestamp with time zone NOT NULL,
                CONSTRAINT pk_historias_clinicas PRIMARY KEY (paciente_id)
            );

            CREATE TABLE IF NOT EXISTS public.historia_clinica_evoluciones (
                "Id"                    uuid                     NOT NULL,
                paciente_id             uuid                     NOT NULL,
                consulta_slot_id        uuid,
                medico_id               integer                  NOT NULL,
                autor_perfil_id         uuid                     NOT NULL,
                fecha_clinica           date                     NOT NULL,
                titulo                  text,
                nota                    text                     NOT NULL,
                diagnostico_impresion   text,
                indicaciones            text,
                created_at              timestamp with time zone NOT NULL,
                updated_at              timestamp with time zone NOT NULL,
                CONSTRAINT pk_historia_clinica_evoluciones PRIMARY KEY ("Id")
            );

            CREATE INDEX IF NOT EXISTS idx_historia_clinica_evoluciones_paciente_fecha
                ON public.historia_clinica_evoluciones (paciente_id, fecha_clinica, created_at);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS public.historia_clinica_evoluciones;
            DROP TABLE IF EXISTS public.historias_clinicas;
            """);
    }
}
