using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace map2stl.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Username = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    Role = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Models",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    GLBData = table.Column<byte[]>(type: "BLOB", nullable: false),
                    STLData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Description = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    UserId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ParentId = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    SouthLat = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    WestLng = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    NorthLat = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    EastLng = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    zFactor = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    meshReduceFactor = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    estimateSize = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    format = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Models", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Models_Models_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Models_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Models_ParentId",
                table: "Models",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Models_UserId",
                table: "Models",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Models");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
