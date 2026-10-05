using Microsoft.EntityFrameworkCore;
using StudentResourceTracker.Models;

namespace StudentResourceTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Student> Students { get; set; }
    public DbSet<Resource> Resources { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Mathematics",    Description = "Math-related resources" },
            new Category { Id = 2, Name = "Science",        Description = "Science resources" },
            new Category { Id = 3, Name = "Literature",     Description = "English & literature" },
            new Category { Id = 4, Name = "Programming",    Description = "Coding & software" },
            new Category { Id = 5, Name = "History",        Description = "History resources" },
            new Category { Id = 6, Name = "General",        Description = "Miscellaneous" }
        );

        // Seed students
        modelBuilder.Entity<Student>().HasData(
            new Student
            {
                Id = 1, FirstName = "Alice", LastName = "Johnson",
                Email = "alice@example.com", StudentNumber = "S001",
                Course = "Computer Science", EnrolledOn = new DateTime(2024, 9, 1)
            },
            new Student
            {
                Id = 2, FirstName = "Bob", LastName = "Smith",
                Email = "bob@example.com", StudentNumber = "S002",
                Course = "Mathematics", EnrolledOn = new DateTime(2024, 9, 1)
            }
        );

        // Seed resources
        modelBuilder.Entity<Resource>().HasData(
            new Resource
            {
                Id = 1, Title = "Introduction to Algorithms",
                Description = "MIT OpenCourseWare — classic algorithms course",
                Url = "https://ocw.mit.edu/courses/6-006-introduction-to-algorithms-spring-2020/",
                Type = ResourceType.Link, CategoryId = 4, StudentId = 1,
                AddedOn = new DateTime(2024, 9, 5), IsCompleted = false
            },
            new Resource
            {
                Id = 2, Title = "Calculus for Beginners",
                Description = "Khan Academy calculus series",
                Url = "https://www.khanacademy.org/math/calculus-1",
                Type = ResourceType.Video, CategoryId = 1, StudentId = 2,
                AddedOn = new DateTime(2024, 9, 6), IsCompleted = true
            }
        );
    }
}
