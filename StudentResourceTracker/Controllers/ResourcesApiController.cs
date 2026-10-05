using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentResourceTracker.Data;
using StudentResourceTracker.Models;

namespace StudentResourceTracker.Controllers;

[ApiController]
[Route("api/resources")]
public class ResourcesApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public ResourcesApiController(AppDbContext context) => _context = context;

    // GET /api/resources?studentId=1&categoryId=2&search=abc&completed=true
    [HttpGet]
    public async Task<IActionResult> GetAll(int? studentId, int? categoryId, string? search, bool? completed)
    {
        var query = _context.Resources.AsQueryable();

        if (studentId.HasValue) query = query.Where(r => r.StudentId == studentId);
        if (categoryId.HasValue) query = query.Where(r => r.CategoryId == categoryId);
        if (completed.HasValue) query = query.Where(r => r.IsCompleted == completed.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.Title.Contains(search) ||
                                     (r.Description != null && r.Description.Contains(search)));

        var items = await query
            .OrderByDescending(r => r.AddedOn)
            .Select(r => new
            {
                r.Id, r.Title, r.Description, r.Url, r.Type, r.AddedOn, r.IsCompleted,
                r.StudentId, r.CategoryId,
                StudentName = r.Student != null ? r.Student.FirstName + " " + r.Student.LastName : null,
                CategoryName = r.Category != null ? r.Category.Name : null
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/resources/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var r = await _context.Resources.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return r is null ? NotFound() : Ok(r);
    }

    // POST /api/resources
    [HttpPost]
    public async Task<IActionResult> Create(Resource resource)
    {
        resource.Id = 0;
        resource.AddedOn = DateTime.UtcNow;
        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetOne), new { id = resource.Id }, resource);
    }

    // PUT /api/resources/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Resource resource)
    {
        if (id != resource.Id) return BadRequest();
        if (!await _context.Resources.AnyAsync(r => r.Id == id)) return NotFound();

        _context.Update(resource);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/resources/5/toggle
    [HttpPost("{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var resource = await _context.Resources.FindAsync(id);
        if (resource is null) return NotFound();

        resource.IsCompleted = !resource.IsCompleted;
        await _context.SaveChangesAsync();
        return Ok(new { resource.Id, resource.IsCompleted });
    }

    // DELETE /api/resources/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var resource = await _context.Resources.FindAsync(id);
        if (resource is null) return NotFound();

        _context.Resources.Remove(resource);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}