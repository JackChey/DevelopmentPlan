using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntityConfigurations_20260621_01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_app_orders_app_users_UserId",
                table: "app_orders");

            migrationBuilder.DropIndex(
                name: "IX_app_orders_UserId",
                table: "app_orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_app_orders_UserId",
                table: "app_orders",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_app_orders_app_users_UserId",
                table: "app_orders",
                column: "UserId",
                principalTable: "app_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
