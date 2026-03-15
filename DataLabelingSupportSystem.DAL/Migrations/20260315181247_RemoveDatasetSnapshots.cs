using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLabelingSupportSystem.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDatasetSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dataset_snapshots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dataset_snapshots",
                columns: table => new
                {
                    SnapshotId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SnapshotData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VersionName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dataset_snapshots", x => x.SnapshotId);
                    table.ForeignKey(
                        name: "FK_dataset_snapshots_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dataset_snapshots_ProjectId",
                table: "dataset_snapshots",
                column: "ProjectId");
        }
    }
}
