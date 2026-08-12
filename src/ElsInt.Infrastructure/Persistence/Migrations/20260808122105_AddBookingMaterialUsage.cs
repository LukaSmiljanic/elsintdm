using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElsInt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingMaterialUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingMaterialUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingMaterialUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingMaterialUsages_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingMaterialUsages_ServiceBookings_ServiceBookingId",
                        column: x => x.ServiceBookingId,
                        principalTable: "ServiceBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingMaterialUsages_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingMaterialUsages_MaterialId",
                table: "BookingMaterialUsages",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingMaterialUsages_ServiceBookingId",
                table: "BookingMaterialUsages",
                column: "ServiceBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingMaterialUsages_WarehouseId",
                table: "BookingMaterialUsages",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingMaterialUsages");
        }
    }
}
