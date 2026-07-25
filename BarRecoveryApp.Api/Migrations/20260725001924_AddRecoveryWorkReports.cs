using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarRecoveryApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRecoveryWorkReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recovery_work_reports",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    WorkDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ShiftName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recovery_work_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "recovery_work_report_categories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    RecoveryWorkReportId = table.Column<string>(type: "text", nullable: false),
                    WorkType = table.Column<int>(type: "integer", nullable: false),
                    PlantId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    BarTypeId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    BarsWorkedCount = table.Column<int>(type: "integer", nullable: false),
                    ExportLabel = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recovery_work_report_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recovery_work_report_categories_recovery_work_reports_Recov~",
                        column: x => x.RecoveryWorkReportId,
                        principalTable: "recovery_work_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recovery_work_activities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    RecoveryWorkReportCategoryId = table.Column<string>(type: "text", nullable: false),
                    ActivityId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    HoursWorked = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recovery_work_activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recovery_work_activities_recovery_work_report_categories_Re~",
                        column: x => x.RecoveryWorkReportCategoryId,
                        principalTable: "recovery_work_report_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recovery_work_supplies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    RecoveryWorkReportCategoryId = table.Column<string>(type: "text", nullable: false),
                    SupplyId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Quantity = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recovery_work_supplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recovery_work_supplies_recovery_work_report_categories_Reco~",
                        column: x => x.RecoveryWorkReportCategoryId,
                        principalTable: "recovery_work_report_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recovery_work_activities_RecoveryWorkReportCategoryId",
                table: "recovery_work_activities",
                column: "RecoveryWorkReportCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_recovery_work_report_categories_RecoveryWorkReportId",
                table: "recovery_work_report_categories",
                column: "RecoveryWorkReportId");

            migrationBuilder.CreateIndex(
                name: "IX_recovery_work_reports_UpdatedAtUtc",
                table: "recovery_work_reports",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_recovery_work_supplies_RecoveryWorkReportCategoryId",
                table: "recovery_work_supplies",
                column: "RecoveryWorkReportCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recovery_work_activities");

            migrationBuilder.DropTable(
                name: "recovery_work_supplies");

            migrationBuilder.DropTable(
                name: "recovery_work_report_categories");

            migrationBuilder.DropTable(
                name: "recovery_work_reports");
        }
    }
}
