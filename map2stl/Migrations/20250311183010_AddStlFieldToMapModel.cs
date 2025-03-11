using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace map2stl.Migrations
{
    /// <inheritdoc />
    public partial class AddStlFieldToMapModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Data",
                table: "Models");

            migrationBuilder.AddColumn<byte[]>(
                name: "GLBData",
                table: "Models",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "STLData",
                table: "Models",
                type: "BLOB",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GLBData",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "STLData",
                table: "Models");

            migrationBuilder.AddColumn<byte[]>(
                name: "Data",
                table: "Models",
                type: "RAW(2000)",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
