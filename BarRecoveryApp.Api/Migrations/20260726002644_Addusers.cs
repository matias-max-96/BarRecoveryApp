using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarRecoveryApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class Addusers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RoleCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PinHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PinSalt = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsPinEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MustChangePin = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_UpdatedAtUtc",
                table: "users",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
