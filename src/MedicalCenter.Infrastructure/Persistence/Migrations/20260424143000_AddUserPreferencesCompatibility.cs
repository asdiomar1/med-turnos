using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferencesCompatibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS public.user_preferences (
                    "Id" uuid NOT NULL,
                    "UserId" uuid NOT NULL,
                    "Theme" character varying(50) NOT NULL,
                    "CustomColorsJson" jsonb NULL,
                    "TurnosLayout" character varying(50) NOT NULL,
                    "FontScale" numeric(5,2) NOT NULL DEFAULT 1.0,
                    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                    CONSTRAINT "PK_user_preferences" PRIMARY KEY ("UserId"),
                    CONSTRAINT "FK_user_preferences_users_UserId"
                        FOREIGN KEY ("UserId") REFERENCES public.users ("Id") ON DELETE CASCADE
                );

                ALTER TABLE public.user_preferences
                ADD COLUMN IF NOT EXISTS "Id" uuid;

                UPDATE public.user_preferences
                SET "Id" = "UserId"
                WHERE "Id" IS NULL;

                ALTER TABLE public.user_preferences
                ALTER COLUMN "Id" SET NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS public.user_preferences;
                """);
        }
    }
}
