using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comillas.AITradingSimulator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackedSymbolCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "TrackedSymbol",
                type: "TEXT",
                maxLength: 8,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "TrackedSymbol");
        }
    }
}
