using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnsenameLaPasta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashMovement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashMovement", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashMovement_CreatedAt",
                table: "CashMovement",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashMovement");
        }
    }
}
