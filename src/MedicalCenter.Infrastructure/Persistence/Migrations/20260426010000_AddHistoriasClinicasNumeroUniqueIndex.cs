using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations;

public partial class AddHistoriasClinicasNumeroUniqueIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            -- Reassign sequential numbers to rows that share a duplicate numero,
            -- preserving the lowest numero for whichever row was inserted first (by ctid).
            WITH duplicates AS (
                SELECT paciente_id,
                       ROW_NUMBER() OVER (ORDER BY ctid) AS rn
                FROM public.historias_clinicas
                WHERE numero IN (
                    SELECT numero FROM public.historias_clinicas
                    GROUP BY numero HAVING COUNT(*) > 1
                )
            ),
            max_existing AS (
                SELECT COALESCE(MAX(numero), 0) AS max_num FROM public.historias_clinicas
            )
            UPDATE public.historias_clinicas h
            SET numero = m.max_num + d.rn
            FROM duplicates d, max_existing m
            WHERE h.paciente_id = d.paciente_id;

            CREATE UNIQUE INDEX IF NOT EXISTS uidx_historias_clinicas_numero
                ON public.historias_clinicas (numero);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS public.uidx_historias_clinicas_numero;");
    }
}
