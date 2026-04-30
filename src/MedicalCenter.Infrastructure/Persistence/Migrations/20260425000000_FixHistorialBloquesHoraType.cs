using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations;

public partial class FixHistorialBloquesHoraType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.historial_bloques
                ALTER COLUMN "Hora" TYPE time without time zone
                USING "Hora"::time without time zone;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.historial_bloques
                ALTER COLUMN "Hora" TYPE character varying(5)
                USING to_char("Hora", 'HH24:MI');
            """);
    }
}
