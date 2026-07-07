using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comillas.AITradingSimulator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionAndDividends : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Commission",
                table: "Trade",
                type: "TEXT",
                nullable: false,
                defaultValue: "0");   // filas previas (HV-050): comisión 0, no "" (rompería el parse decimal)

            migrationBuilder.CreateTable(
                name: "Dividend",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Amount = table.Column<string>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dividend", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dividend_ReceivedAt",
                table: "Dividend",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Dividend_Symbol",
                table: "Dividend",
                column: "Symbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dividend");

            migrationBuilder.DropColumn(
                name: "Commission",
                table: "Trade");
        }
    }
}
