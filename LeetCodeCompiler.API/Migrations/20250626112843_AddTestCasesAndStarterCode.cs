using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeetCodeCompiler.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTestCasesAndStarterCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StarterCode",
                table: "Problems");

            migrationBuilder.CreateTable(
                name: "StarterCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProblemId = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarterCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StarterCodes_Problems_ProblemId",
                        column: x => x.ProblemId,
                        principalTable: "Problems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProblemId = table.Column<int>(type: "int", nullable: false),
                    Input = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedOutput = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestCases_Problems_ProblemId",
                        column: x => x.ProblemId,
                        principalTable: "Problems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StarterCodes_ProblemId",
                table: "StarterCodes",
                column: "ProblemId");

            migrationBuilder.CreateIndex(
                name: "IX_TestCases_ProblemId",
                table: "TestCases",
                column: "ProblemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StarterCodes");

            migrationBuilder.DropTable(
                name: "TestCases");

            migrationBuilder.AddColumn<string>(
                name: "StarterCode",
                table: "Problems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Problems",
                keyColumn: "Id",
                keyValue: 1,
                column: "StarterCode",
                value: "def two_sum(nums, target):\n    # Write your code here\n    pass");

            migrationBuilder.UpdateData(
                table: "Problems",
                keyColumn: "Id",
                keyValue: 2,
                column: "StarterCode",
                value: "def reverse_string(s):\n    # Write your code here\n    pass");
        }
    }
}
