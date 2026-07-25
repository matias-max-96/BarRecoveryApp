using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarRecoveryApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBarReturnReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bar_return_receipts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ReturnDocument = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResponsibleUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bar_return_receipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bar_return_receipt_bars",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    BarReturnReceiptId = table.Column<string>(type: "text", nullable: false),
                    BarId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bar_return_receipt_bars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bar_return_receipt_bars_bar_return_receipts_BarReturnReceip~",
                        column: x => x.BarReturnReceiptId,
                        principalTable: "bar_return_receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bar_return_receipt_bars_BarReturnReceiptId",
                table: "bar_return_receipt_bars",
                column: "BarReturnReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_bar_return_receipts_UpdatedAtUtc",
                table: "bar_return_receipts",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bar_return_receipt_bars");

            migrationBuilder.DropTable(
                name: "bar_return_receipts");
        }
    }
}
