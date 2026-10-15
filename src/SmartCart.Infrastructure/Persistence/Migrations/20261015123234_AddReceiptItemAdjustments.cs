using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCart.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptItemAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParsedName",
                table: "ReceiptItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "ReceiptItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReceiptItemAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptItemAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptItemAdjustments_ReceiptItems_ReceiptItemId",
                        column: x => x.ReceiptItemId,
                        principalTable: "ReceiptItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptItemAdjustments_ReceiptItemId",
                table: "ReceiptItemAdjustments",
                column: "ReceiptItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiptItemAdjustments");

            migrationBuilder.DropColumn(
                name: "ParsedName",
                table: "ReceiptItems");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "ReceiptItems");
        }
    }
}
