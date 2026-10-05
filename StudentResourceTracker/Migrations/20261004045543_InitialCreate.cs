using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StudentResourceTracker.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StudentNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Course = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    EnrolledOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    AddedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    StudentId = table.Column<int>(type: "INTEGER", nullable: true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resources_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resources_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Math-related resources", "Mathematics" },
                    { 2, "Science resources", "Science" },
                    { 3, "English & literature", "Literature" },
                    { 4, "Coding & software", "Programming" },
                    { 5, "History resources", "History" },
                    { 6, "Miscellaneous", "General" }
                });

            migrationBuilder.InsertData(
                table: "Students",
                columns: new[] { "Id", "Course", "Email", "EnrolledOn", "FirstName", "LastName", "StudentNumber" },
                values: new object[,]
                {
                    { 1, "Computer Science", "alice@example.com", new DateTime(2024, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Alice", "Johnson", "S001" },
                    { 2, "Mathematics", "bob@example.com", new DateTime(2024, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Bob", "Smith", "S002" }
                });

            migrationBuilder.InsertData(
                table: "Resources",
                columns: new[] { "Id", "AddedOn", "CategoryId", "Description", "IsCompleted", "StudentId", "Title", "Type", "Url" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 9, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "MIT OpenCourseWare — classic algorithms course", false, 1, "Introduction to Algorithms", 3, "https://ocw.mit.edu/courses/6-006-introduction-to-algorithms-spring-2020/" },
                    { 2, new DateTime(2024, 9, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Khan Academy calculus series", true, 2, "Calculus for Beginners", 1, "https://www.khanacademy.org/math/calculus-1" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_CategoryId",
                table: "Resources",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_StudentId",
                table: "Resources",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Students");
        }
    }
}
