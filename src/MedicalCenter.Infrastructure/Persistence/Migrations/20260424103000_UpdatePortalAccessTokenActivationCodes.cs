using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations;

public partial class UpdatePortalAccessTokenActivationCodes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.portal_access_tokens
                ADD COLUMN IF NOT EXISTS "IssuedToMasked" character varying(100) NULL;

            ALTER TABLE public.portal_access_tokens
                ADD COLUMN IF NOT EXISTS "LastAttemptAt" timestamp with time zone NULL;

            DROP INDEX IF EXISTS "IX_portal_access_tokens_TokenHash";

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_portal_access_tokens_TokenHash"
                ON public.portal_access_tokens ("TokenHash")
                WHERE "UsedAt" IS NULL AND "RevokedAt" IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_portal_access_tokens_TokenHash",
            table: "portal_access_tokens");

        migrationBuilder.DropColumn(
            name: "IssuedToMasked",
            table: "portal_access_tokens");

        migrationBuilder.DropColumn(
            name: "LastAttemptAt",
            table: "portal_access_tokens");

        migrationBuilder.CreateIndex(
            name: "IX_portal_access_tokens_TokenHash",
            table: "portal_access_tokens",
            column: "TokenHash",
            unique: true);
    }
}
