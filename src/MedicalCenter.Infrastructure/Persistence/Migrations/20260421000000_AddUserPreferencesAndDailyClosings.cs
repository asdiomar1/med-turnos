using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations
{
    public partial class AddUserPreferencesAndDailyClosings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS public.user_preferences (
                    "UserId" uuid NOT NULL,
                    "Theme" character varying(50) NOT NULL DEFAULT 'light',
                    "CustomColorsJson" jsonb NULL,
                    "TurnosLayout" character varying(50) NOT NULL DEFAULT 'grid',
                    "FontScale" numeric(5,2) NOT NULL DEFAULT 1.0,
                    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                    CONSTRAINT "PK_user_preferences" PRIMARY KEY ("UserId"),
                    CONSTRAINT "FK_user_preferences_users_UserId"
                        FOREIGN KEY ("UserId") REFERENCES public.users ("Id") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS public.daily_closings (
                    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
                    "Fecha" date NOT NULL,
                    "Status" integer NOT NULL DEFAULT 0,
                    "DetallesJson" jsonb NULL,
                    "CreatedByUserId" uuid NOT NULL,
                    "ConfirmedByUserId" uuid NULL,
                    "ReopenedByUserId" uuid NULL,
                    "MotivoReapertura" character varying(500) NULL,
                    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                    "ConfirmedAt" timestamp with time zone NULL,
                    "ReopenedAt" timestamp with time zone NULL,
                    CONSTRAINT "PK_daily_closings" PRIMARY KEY ("Id")
                );

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_daily_closings_Fecha"
                    ON public.daily_closings ("Fecha");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_closings");

            migrationBuilder.DropTable(
                name: "user_preferences");
        }
    }
}
