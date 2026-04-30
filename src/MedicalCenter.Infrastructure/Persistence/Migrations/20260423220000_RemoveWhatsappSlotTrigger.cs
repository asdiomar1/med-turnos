using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations;

public partial class RemoveWhatsappSlotTrigger : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            drop trigger if exists whatsapp_on_slot_ocupado on public.slots;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            create trigger whatsapp_on_slot_ocupado
            after insert or update on public.slots
            for each row execute function public.whatsapp_on_slot_ocupado();
            """);
    }
}
