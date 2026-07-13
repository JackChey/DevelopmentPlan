using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntityConfigurations_AddStockRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_in_records",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductCodeSnapshot = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Remark = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModifiedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_in_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_in_records_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "stock_out_records",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductCodeSnapshot = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Remark = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModifiedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_out_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_out_records_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_stock_in_records_CreatedAt_Id",
                table: "stock_in_records",
                columns: new[] { "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_in_records_CreatedByUserId",
                table: "stock_in_records",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_in_records_LastModifiedByUserId",
                table: "stock_in_records",
                column: "LastModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_in_records_ProductCodeSnapshot",
                table: "stock_in_records",
                column: "ProductCodeSnapshot");

            migrationBuilder.CreateIndex(
                name: "IX_stock_in_records_ProductId",
                table: "stock_in_records",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_in_records_ProductId_CreatedAt",
                table: "stock_in_records",
                columns: new[] { "ProductId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_out_records_CreatedAt_Id",
                table: "stock_out_records",
                columns: new[] { "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_out_records_CreatedByUserId",
                table: "stock_out_records",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_out_records_LastModifiedByUserId",
                table: "stock_out_records",
                column: "LastModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_out_records_ProductCodeSnapshot",
                table: "stock_out_records",
                column: "ProductCodeSnapshot");

            migrationBuilder.CreateIndex(
                name: "IX_stock_out_records_ProductId",
                table: "stock_out_records",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_out_records_ProductId_CreatedAt",
                table: "stock_out_records",
                columns: new[] { "ProductId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_in_records");

            migrationBuilder.DropTable(
                name: "stock_out_records");
        }
    }
}
