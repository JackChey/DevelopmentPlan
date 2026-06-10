using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntityConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_products_CreatedAt_Id",
                table: "products",
                columns: new[] { "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_app_users_CreatedAt_Id",
                table: "app_users",
                columns: new[] { "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_app_orders_CreatedAt_Id",
                table: "app_orders",
                columns: new[] { "CreatedAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_CreatedAt_Id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_app_users_CreatedAt_Id",
                table: "app_users");

            migrationBuilder.DropIndex(
                name: "IX_app_orders_CreatedAt_Id",
                table: "app_orders");
        }
    }
}
