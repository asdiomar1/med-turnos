using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations;

public partial class AddNotasPaciente : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.notas_paciente (
                "Id"          uuid                     NOT NULL,
                paciente_id   uuid                     NOT NULL,
                autor_id      uuid                     NOT NULL,
                mensaje       text                     NOT NULL,
                created_at    timestamp with time zone NOT NULL,
                CONSTRAINT pk_notas_paciente PRIMARY KEY ("Id")
            );

            CREATE INDEX IF NOT EXISTS notas_paciente_paciente_id_idx ON public.notas_paciente (paciente_id);
            CREATE INDEX IF NOT EXISTS notas_paciente_created_at_idx  ON public.notas_paciente (created_at);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS public.notas_paciente;");
    }
}
