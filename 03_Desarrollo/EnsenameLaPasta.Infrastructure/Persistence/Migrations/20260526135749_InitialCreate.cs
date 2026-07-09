using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnsenameLaPasta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketTick",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Price = table.Column<string>(type: "TEXT", nullable: false),
                    Volume = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketTick", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioSnapshot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Capital = table.Column<string>(type: "TEXT", nullable: false),
                    RealizedPnL = table.Column<string>(type: "TEXT", nullable: false),
                    UnrealizedPnL = table.Column<string>(type: "TEXT", nullable: false),
                    OpenPositions = table.Column<int>(type: "INTEGER", nullable: false),
                    ClosedTrades = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioSnapshot", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EntryPrice = table.Column<string>(type: "TEXT", nullable: false),
                    ExitPrice = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trade", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketTick_Symbol_Timestamp",
                table: "MarketTick",
                columns: ["Symbol", "Timestamp"]);

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioSnapshot_Timestamp",
                table: "PortfolioSnapshot",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Trade_Status",
                table: "Trade",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Trade_Symbol_CreatedAt",
                table: "Trade",
                columns: ["Symbol", "CreatedAt"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketTick");

            migrationBuilder.DropTable(
                name: "PortfolioSnapshot");

            migrationBuilder.DropTable(
                name: "Trade");
        }
    }
}
