using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLabelingSupportSystem.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddOcclusionAndTruncation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOccluded",
                table: "annotations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTruncated",
                table: "annotations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOccluded",
                table: "annotations");

            migrationBuilder.DropColumn(
                name: "IsTruncated",
                table: "annotations");
        }
    }
}
