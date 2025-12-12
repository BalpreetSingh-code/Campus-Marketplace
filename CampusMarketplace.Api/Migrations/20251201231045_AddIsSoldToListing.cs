using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusMarketplace.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSoldToListing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Listings_ListingId",
                table: "Orders");

            migrationBuilder.AlterColumn<int>(
                name: "ListingId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSold",
                table: "Listings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Listings_ListingId",
                table: "Orders",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Listings_ListingId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsSold",
                table: "Listings");

            migrationBuilder.AlterColumn<int>(
                name: "ListingId",
                table: "Orders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Listings_ListingId",
                table: "Orders",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
