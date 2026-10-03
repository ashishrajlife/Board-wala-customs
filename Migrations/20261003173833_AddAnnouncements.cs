using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ValousWorld.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnouncements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoucherCode",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoucherDiscountPercent",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VoucherIsActive",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "VoucherMaxDiscount",
                table: "Products",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VoucherMinOrderValue",
                table: "Products",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoucherValidUntil",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    AnnouncementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.AnnouncementId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_DisplayOrder",
                table: "Announcements",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_IsActive",
                table: "Announcements",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropColumn(
                name: "VoucherCode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VoucherDiscountPercent",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VoucherIsActive",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VoucherMaxDiscount",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VoucherMinOrderValue",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VoucherValidUntil",
                table: "Products");
        }
    }
}
