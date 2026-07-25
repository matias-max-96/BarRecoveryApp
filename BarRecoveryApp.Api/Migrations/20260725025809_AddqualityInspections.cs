using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarRecoveryApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddqualityInspections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quality_inspections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    BarId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    InspectorUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    InspectionAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecoveryCountAtInspection = table.Column<int>(type: "integer", nullable: false),
                    CanBeRecovered = table.Column<bool>(type: "boolean", nullable: false),
                    MustBeDisposed = table.Column<bool>(type: "boolean", nullable: false),
                    IsApprovedForShipment = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quality_inspections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quality_inspection_attribute_values",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    QualityInspectionId = table.Column<string>(type: "text", nullable: false),
                    BarId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    AttributeDefinitionId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    AttributeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AttributeName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DataType = table.Column<int>(type: "integer", nullable: false),
                    WasMeasured = table.Column<bool>(type: "boolean", nullable: false),
                    ValueText = table.Column<string>(type: "text", nullable: true),
                    ValueNumber = table.Column<double>(type: "double precision", nullable: true),
                    ValueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValueBool = table.Column<bool>(type: "boolean", nullable: true),
                    IsOutOfRange = table.Column<bool>(type: "boolean", nullable: true),
                    MinValueAtInspection = table.Column<double>(type: "double precision", nullable: true),
                    MaxValueAtInspection = table.Column<double>(type: "double precision", nullable: true),
                    UnitAtInspection = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ToleranceTextAtInspection = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quality_inspection_attribute_values", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quality_inspection_attribute_values_quality_inspections_Qua~",
                        column: x => x.QualityInspectionId,
                        principalTable: "quality_inspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quality_inspection_attribute_values_QualityInspectionId",
                table: "quality_inspection_attribute_values",
                column: "QualityInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_quality_inspections_UpdatedAtUtc",
                table: "quality_inspections",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quality_inspection_attribute_values");

            migrationBuilder.DropTable(
                name: "quality_inspections");
        }
    }
}
