using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarRecoveryApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bars",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    BarNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PlantId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    BarTypeId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CurrentStatus = table.Column<int>(type: "integer", nullable: false),
                    RecoveryCount = table.Column<int>(type: "integer", nullable: false),
                    IsDisposed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bars", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bars_PlantId_BarTypeId_BarNumber",
                table: "bars",
                columns: new[] { "PlantId", "BarTypeId", "BarNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bars_UpdatedAtUtc",
                table: "bars",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bars");
        }
    }
}
