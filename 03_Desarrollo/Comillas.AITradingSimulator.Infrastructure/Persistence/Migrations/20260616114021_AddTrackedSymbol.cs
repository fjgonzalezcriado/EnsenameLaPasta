using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comillas.AITradingSimulator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackedSymbol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackedSymbol",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedSymbol", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackedSymbol_Symbol",
                table: "TrackedSymbol",
                column: "Symbol",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackedSymbol");
        }
    }
}
