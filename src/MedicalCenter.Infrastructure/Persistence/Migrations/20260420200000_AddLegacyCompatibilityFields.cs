using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyCompatibilityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OptInSource",
                table: "patients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsBloqueCompleto",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EsTanda",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReferidoTercero",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReferenteId",
                table: "appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModalidadCobro",
                table: "appointments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "particular");

            migrationBuilder.AddColumn<int>(
                name: "ObraSocialId",
                table: "appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroAutorizacion",
                table: "appointments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SesionesAutorizadas",
                table: "appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CicloObraSocialId",
                table: "appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IniciarNuevoCicloObraSocial",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ConvenioCorroborado",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MedicoId",
                table: "appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsNuevoIngreso",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EsMonoxido",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MonoxidoOrdenMedica",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MonoxidoResumenClinico",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "OptInSource", table: "patients");

            migrationBuilder.DropColumn(name: "EsBloqueCompleto", table: "appointments");
            migrationBuilder.DropColumn(name: "EsTanda", table: "appointments");
            migrationBuilder.DropColumn(name: "ReferidoTercero", table: "appointments");
            migrationBuilder.DropColumn(name: "ReferenteId", table: "appointments");
            migrationBuilder.DropColumn(name: "ModalidadCobro", table: "appointments");
            migrationBuilder.DropColumn(name: "ObraSocialId", table: "appointments");
            migrationBuilder.DropColumn(name: "NumeroAutorizacion", table: "appointments");
            migrationBuilder.DropColumn(name: "SesionesAutorizadas", table: "appointments");
            migrationBuilder.DropColumn(name: "CicloObraSocialId", table: "appointments");
            migrationBuilder.DropColumn(name: "IniciarNuevoCicloObraSocial", table: "appointments");
            migrationBuilder.DropColumn(name: "ConvenioCorroborado", table: "appointments");
            migrationBuilder.DropColumn(name: "MedicoId", table: "appointments");
            migrationBuilder.DropColumn(name: "EsNuevoIngreso", table: "appointments");
            migrationBuilder.DropColumn(name: "EsMonoxido", table: "appointments");
            migrationBuilder.DropColumn(name: "MonoxidoOrdenMedica", table: "appointments");
            migrationBuilder.DropColumn(name: "MonoxidoResumenClinico", table: "appointments");
        }
    }
}
