using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachPulse.Infrastructure.Migrations;

[DbContext(typeof(CoachPulseDbContext))]
[Migration("20261019090000_DefaultTenantLanguageEnglish")]
public partial class DefaultTenantLanguageEnglish : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "DefaultLanguage",
            table: "Tenants",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "en",
            oldClrType: typeof(string),
            oldType: "character varying(16)",
            oldMaxLength: 16,
            oldDefaultValue: "ar");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "DefaultLanguage",
            table: "Tenants",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "ar",
            oldClrType: typeof(string),
            oldType: "character varying(16)",
            oldMaxLength: 16,
            oldDefaultValue: "en");
    }
}
