using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations;

public partial class AddConsultationsAndOutOfHours : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.consultas_horarios_config (
                "Id" integer NOT NULL,
                "Hora" character varying(5) NOT NULL,
                "Activo" boolean NOT NULL DEFAULT true,
                "Orden" integer NOT NULL DEFAULT 0,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                CONSTRAINT "PK_consultas_horarios_config" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_consultas_horarios_config_Hora"
                ON public.consultas_horarios_config ("Hora");

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_consultas_horarios_config_Orden"
                ON public.consultas_horarios_config ("Orden");

            CREATE TABLE IF NOT EXISTS public.consultas_slots (
                "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
                "Fecha" date NOT NULL,
                "Hora" time without time zone NOT NULL,
                "Estado" character varying(20) NOT NULL DEFAULT 'Libre',
                "PacienteId" uuid NULL,
                "MedicoId" integer NULL,
                "MotivoCancelacion" character varying(500) NULL,
                "ObservacionesAdmin" character varying(4000) NULL,
                "ConfirmadoAt" timestamp with time zone NULL,
                "ConfirmadoPor" uuid NULL,
                "CerradoAt" timestamp with time zone NULL,
                "CerradoPor" uuid NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                CONSTRAINT "PK_consultas_slots" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_consultas_slots_Fecha_Hora"
                ON public.consultas_slots ("Fecha", "Hora");

            CREATE INDEX IF NOT EXISTS "IX_consultas_slots_Estado"
                ON public.consultas_slots ("Estado");

            CREATE INDEX IF NOT EXISTS "IX_consultas_slots_PacienteId"
                ON public.consultas_slots ("PacienteId");

            CREATE INDEX IF NOT EXISTS "IX_consultas_slots_MedicoId"
                ON public.consultas_slots ("MedicoId");

            CREATE INDEX IF NOT EXISTS "IX_consultas_slots_ConfirmadoPor"
                ON public.consultas_slots ("ConfirmadoPor");

            CREATE INDEX IF NOT EXISTS "IX_consultas_slots_CerradoPor"
                ON public.consultas_slots ("CerradoPor");

            CREATE TABLE IF NOT EXISTS public.sesiones (
                "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
                "PacienteId" uuid NULL,
                "SlotId" uuid NULL,
                "Fecha" date NOT NULL,
                "Hora" time without time zone NOT NULL,
                "CamaraId" integer NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                "ModalidadCobro" character varying(20) NOT NULL DEFAULT 'particular',
                "ObraSocialId" integer NULL,
                "CierreId" uuid NULL,
                "NumeroAutorizacion" character varying(100) NULL,
                "SesionesAutorizadas" integer NULL,
                "CicloObraSocialId" uuid NULL,
                CONSTRAINT "PK_sesiones" PRIMARY KEY ("Id")
            );

            CREATE INDEX IF NOT EXISTS "IX_sesiones_Fecha"
                ON public.sesiones ("Fecha");

            CREATE INDEX IF NOT EXISTS "IX_sesiones_PacienteId"
                ON public.sesiones ("PacienteId");

            CREATE INDEX IF NOT EXISTS "IX_sesiones_SlotId"
                ON public.sesiones ("SlotId");

            CREATE TABLE IF NOT EXISTS public.turnos_fuera_horario (
                "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
                "Fecha" date NOT NULL,
                "Hora" time without time zone NOT NULL,
                "PacienteId" uuid NOT NULL,
                "Notas" text NULL,
                "CreadoPor" uuid NOT NULL,
                "OperadorCamaraId" uuid NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                "EsMonoxido" boolean NOT NULL DEFAULT false,
                "MonoxidoOrdenMedica" boolean NOT NULL DEFAULT false,
                "MonoxidoResumenClinico" boolean NOT NULL DEFAULT false,
                "MonoxidoMedicoId" integer NULL,
                CONSTRAINT "PK_turnos_fuera_horario" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_turnos_fuera_horario_Fecha_Hora_PacienteId"
                ON public.turnos_fuera_horario ("Fecha", "Hora", "PacienteId");

            CREATE INDEX IF NOT EXISTS "IX_turnos_fuera_horario_PacienteId"
                ON public.turnos_fuera_horario ("PacienteId");

            CREATE INDEX IF NOT EXISTS "IX_turnos_fuera_horario_MonoxidoMedicoId"
                ON public.turnos_fuera_horario ("MonoxidoMedicoId");

            CREATE TABLE IF NOT EXISTS public.historial_bloques (
                "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
                "Fecha" date NOT NULL,
                "Hora" time without time zone NOT NULL,
                "CamaraId" integer NULL,
                "SlotId" uuid NULL,
                "Lugar" integer NULL,
                "Accion" character varying(100) NOT NULL,
                "PacienteId" uuid NULL,
                "RealizadoPor" uuid NULL,
                "Motivo" text NULL,
                "ReferidoTercero" boolean NOT NULL DEFAULT false,
                "ModalidadCobro" character varying(20) NOT NULL DEFAULT 'particular',
                "ObraSocialId" integer NULL,
                "NumeroAutorizacion" character varying(100) NULL,
                "ObraSocialValidadaPor" uuid NULL,
                "ObraSocialValidadaAt" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                "MedicoId" integer NULL,
                "EsNuevoIngreso" boolean NOT NULL DEFAULT false,
                "ReferenteId" integer NULL,
                "TandaId" uuid NULL,
                "SesionesAutorizadas" integer NULL,
                "CicloObraSocialId" uuid NULL,
                CONSTRAINT "PK_historial_bloques" PRIMARY KEY ("Id")
            );

            CREATE INDEX IF NOT EXISTS "IX_historial_bloques_CamaraId_Fecha_Hora"
                ON public.historial_bloques ("Fecha", "Hora", "CamaraId");

            CREATE INDEX IF NOT EXISTS "IX_historial_bloques_SlotId"
                ON public.historial_bloques ("SlotId");

            CREATE INDEX IF NOT EXISTS "IX_historial_bloques_TandaId"
                ON public.historial_bloques ("TandaId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "historial_bloques");
        migrationBuilder.DropTable(name: "turnos_fuera_horario");
        migrationBuilder.DropTable(name: "sesiones");
        migrationBuilder.DropTable(name: "consultas_slots");
        migrationBuilder.DropTable(name: "consultas_horarios_config");
    }
}
