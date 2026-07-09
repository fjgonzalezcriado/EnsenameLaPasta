using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnsenameLaPasta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashMovementCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "CashMovement",
                type: "TEXT",
                maxLength: 8,
                nullable: false,
                defaultValue: "EUR");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "CashMovement");
        }
    }
}
