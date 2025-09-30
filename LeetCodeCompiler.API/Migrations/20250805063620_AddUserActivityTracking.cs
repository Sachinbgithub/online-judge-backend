using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeetCodeCompiler.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserActivityTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoreQuestionResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProblemId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    TotalTestCases = table.Column<int>(type: "int", nullable: false),
                    PassedTestCases = table.Column<int>(type: "int", nullable: false),
                    FailedTestCases = table.Column<int>(type: "int", nullable: false),
                    LanguageUsed = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FinalCodeSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedHelp = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    LastSubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoreQuestionResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoreQuestionResults_Problems_ProblemId",
                        column: x => x.ProblemId,
                        principalTable: "Problems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCodingActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProblemId = table.Column<int>(type: "int", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    TestType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeTakenSeconds = table.Column<double>(type: "float", nullable: false),
                    LanguageSwitchCount = table.Column<int>(type: "int", nullable: false),
                    EraseCount = table.Column<int>(type: "int", nullable: false),
                    SaveCount = table.Column<int>(type: "int", nullable: false),
                    RunClickCount = table.Column<int>(type: "int", nullable: false),
                    SubmitClickCount = table.Column<int>(type: "int", nullable: false),
                    LoginLogoutCount = table.Column<int>(type: "int", nullable: false),
                    IsSessionAbandoned = table.Column<bool>(type: "bit", nullable: false),
                    PassedTestCaseIDs = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailedTestCaseIDs = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCodingActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCodingActivityLogs_Problems_ProblemId",
                        column: x => x.ProblemId,
                        principalTable: "Problems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoreTestCaseResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoreQuestionResultId = table.Column<int>(type: "int", nullable: false),
                    ProblemId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TestCaseId = table.Column<int>(type: "int", nullable: false),
                    IsPassed = table.Column<bool>(type: "bit", nullable: false),
                    UserOutput = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedOutput = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExecutionTime = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoreTestCaseResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoreTestCaseResults_CoreQuestionResults_CoreQuestionResultId",
                        column: x => x.CoreQuestionResultId,
                        principalTable: "CoreQuestionResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoreQuestionResults_ProblemId",
                table: "CoreQuestionResults",
                column: "ProblemId");

            migrationBuilder.CreateIndex(
                name: "IX_CoreTestCaseResults_CoreQuestionResultId",
                table: "CoreTestCaseResults",
                column: "CoreQuestionResultId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCodingActivityLogs_ProblemId",
                table: "UserCodingActivityLogs",
                column: "ProblemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoreTestCaseResults");

            migrationBuilder.DropTable(
                name: "UserCodingActivityLogs");

            migrationBuilder.DropTable(
                name: "CoreQuestionResults");
        }
    }
}
