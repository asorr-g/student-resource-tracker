using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentResourceTracker.Data;
using StudentResourceTracker.Models;

namespace StudentResourceTracker.Controllers;

public class StudentsController : Controller
{
    private readonly AppDbContext _context;

    public StudentsController(AppDbContext context) => _context = context;

    // GET: Students
    public async Task<IActionResult> Index(string? search)
    {
        ViewBag.Search = search;
        var students = _context.Students
            .Include(s => s.Resources)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            students = students.Where(s =>
                s.FirstName.Contains(search) ||
                s.LastName.Contains(search) ||
                s.Email.Contains(search) ||
                (s.Course != null && s.Course.Contains(search)));

        return View(await students.OrderBy(s => s.LastName).ToListAsync());
    }

    // GET: Students/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var student = await _context.Students
            .Include(s => s.Resources)
                .ThenInclude(r => r.Category)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student is null) return NotFound();
        return View(student);
    }

    // GET: Students/Create
    public IActionResult Create() => View();

    // POST: Students/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Student student)
    {
        if (!ModelState.IsValid) return View(student);

        student.EnrolledOn = DateTime.UtcNow;
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Student {student.FullName} added successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Students/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student is null) return NotFound();
        return View(student);
    }

    // POST: Students/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Student student)
    {
        if (id != student.Id) return BadRequest();
        if (!ModelState.IsValid) return View(student);

        try
        {
            _context.Update(student);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Students.AnyAsync(s => s.Id == id)) return NotFound();
            throw;
        }

        TempData["Success"] = $"Student {student.FullName} updated.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Students/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var student = await _context.Students
            .Include(s => s.Resources)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student is null) return NotFound();
        return View(student);
    }

    // POST: Students/Delete/5
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student is not null)
        {
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Student removed.";
        }
        return RedirectToAction(nameof(Index));
    }
}
