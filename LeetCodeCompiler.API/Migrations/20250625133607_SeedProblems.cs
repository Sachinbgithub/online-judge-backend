using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LeetCodeCompiler.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedProblems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Problems",
                columns: new[] { "Id", "Constraints", "Description", "Examples", "StarterCode", "Title" },
                values: new object[,]
                {
                    { 1, "2 ≤ nums.length ≤ 10⁴\n-10⁹ ≤ nums[i] ≤ 10⁹\n-10⁹ ≤ target ≤ 10⁹\nOnly one valid answer exists.", "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target. You may assume that each input would have exactly one solution, and you may not use the same element twice. You can return the answer in any order.", "Input: nums = [2,7,11,15], target = 9\nOutput: [0,1]\nExplanation: Because nums[0] + nums[1] == 9, we return [0, 1].", "def two_sum(nums, target):\n    # Write your code here\n    pass", "Two Sum" },
                    { 2, "1 ≤ s.length ≤ 10⁵", "Write a function that reverses a string. The input string is given as an array of characters s.", "Input: s = [\"h\",\"e\",\"l\",\"l\",\"o\"]\nOutput: [\"o\",\"l\",\"l\",\"e\",\"h\"]", "def reverse_string(s):\n    # Write your code here\n    pass", "Reverse String" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Problems",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Problems",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
