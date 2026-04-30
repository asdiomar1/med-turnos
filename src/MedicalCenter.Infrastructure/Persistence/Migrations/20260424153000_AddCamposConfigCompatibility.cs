using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCamposConfigCompatibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS public.campos_config (
                    id uuid NOT NULL DEFAULT gen_random_uuid(),
                    nombre text NOT NULL,
                    tipo text NOT NULL,
                    orden integer NOT NULL DEFAULT 0,
                    created_at timestamp with time zone NULL DEFAULT now(),
                    CONSTRAINT campos_config_pkey PRIMARY KEY (id),
                    CONSTRAINT campos_config_tipo_check CHECK (tipo IN ('texto', 'checkbox', 'numero'))
                );

                CREATE INDEX IF NOT EXISTS "IX_campos_config_orden"
                    ON public.campos_config (orden);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS public.campos_config;
                """);
        }
    }
}
