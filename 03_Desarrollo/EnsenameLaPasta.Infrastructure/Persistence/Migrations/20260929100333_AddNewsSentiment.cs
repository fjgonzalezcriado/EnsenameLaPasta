using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnsenameLaPasta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsSentiment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsSentiment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    DateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Score = table.Column<string>(type: "TEXT", nullable: false),
                    SampleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsSentiment", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsSentiment_Symbol_DateUtc",
                table: "NewsSentiment",
                columns: new[] { "Symbol", "DateUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsSentiment");
        }
    }
}
