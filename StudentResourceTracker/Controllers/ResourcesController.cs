using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentResourceTracker.Data;
using StudentResourceTracker.Models;
using StudentResourceTracker.ViewModels;

namespace StudentResourceTracker.Controllers;

public class ResourcesController : Controller
{
    private readonly AppDbContext _context;

    public ResourcesController(AppDbContext context) => _context = context;

    // GET: Resources
    public async Task<IActionResult> Index(ResourceFilterViewModel filter)
    {
        var query = _context.Resources
            .Include(r => r.Student)
            .Include(r => r.Category)
            .AsQueryable();

        if (filter.SelectedStudentId.HasValue)
            query = query.Where(r => r.StudentId == filter.SelectedStudentId);

        if (filter.SelectedCategoryId.HasValue)
            query = query.Where(r => r.CategoryId == filter.SelectedCategoryId);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(r =>
                r.Title.Contains(filter.SearchTerm) ||
                (r.Description != null && r.Description.Contains(filter.SearchTerm)));

        if (filter.ShowCompleted.HasValue)
            query = query.Where(r => r.IsCompleted == filter.ShowCompleted.Value);

        filter.Resources   = await query.OrderByDescending(r => r.AddedOn).ToListAsync();
        filter.Students    = new SelectList(await _context.Students.OrderBy(s => s.LastName).ToListAsync(), "Id", "FullName", filter.SelectedStudentId);
        filter.Categories  = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", filter.SelectedCategoryId);

        return View(filter);
    }

    // GET: Resources/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var resource = await _context.Resources
            .Include(r => r.Student)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (resource is null) return NotFound();
        return View(resource);
    }

    // GET: Resources/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns();
        return View();
    }

    // POST: Resources/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Resource resource)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(resource.StudentId, resource.CategoryId);
            return View(resource);
        }

        resource.AddedOn = DateTime.UtcNow;
        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"\"{resource.Title}\" added successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Resources/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var resource = await _context.Resources.FindAsync(id);
        if (resource is null) return NotFound();

        await PopulateDropdowns(resource.StudentId, resource.CategoryId);
        return View(resource);
    }

    // POST: Resources/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Resource resource)
    {
        if (id != resource.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(resource.StudentId, resource.CategoryId);
            return View(resource);
        }

        try
        {
            _context.Update(resource);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Resources.AnyAsync(r => r.Id == id)) return NotFound();
            throw;
        }

        TempData["Success"] = $"\"{resource.Title}\" updated.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Resources/ToggleComplete/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleComplete(int id)
    {
        var resource = await _context.Resources.FindAsync(id);
        if (resource is null) return NotFound();

        resource.IsCompleted = !resource.IsCompleted;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: Resources/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var resource = await _context.Resources
            .Include(r => r.Student)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (resource is null) return NotFound();
        return View(resource);
    }

    // POST: Resources/Delete/5
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var resource = await _context.Resources.FindAsync(id);
        if (resource is not null)
        {
            _context.Resources.Remove(resource);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Resource removed.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdowns(int? studentId = null, int? categoryId = null)
    {
        ViewBag.Students   = new SelectList(await _context.Students.OrderBy(s => s.LastName).ToListAsync(), "Id", "FullName", studentId);
        ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", categoryId);
        ViewBag.Types      = Enum.GetValues<ResourceType>().Select(t => new SelectListItem
        {
            Value = ((int)t).ToString(),
            Text  = t.ToString()
        });
    }
}
