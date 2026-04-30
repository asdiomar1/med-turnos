using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations;

public partial class AddRoleMetadataFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "roles",
            type: "character varying(250)",
            maxLength: 250,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "active",
            table: "roles",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_system",
            table: "roles",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "is_staff",
            table: "roles",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<string>(
            name: "default_home",
            table: "roles",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: "/usuario");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Description",
            table: "roles");

        migrationBuilder.DropColumn(
            name: "active",
            table: "roles");

        migrationBuilder.DropColumn(
            name: "is_system",
            table: "roles");

        migrationBuilder.DropColumn(
            name: "is_staff",
            table: "roles");

        migrationBuilder.DropColumn(
            name: "default_home",
            table: "roles");
    }
}
