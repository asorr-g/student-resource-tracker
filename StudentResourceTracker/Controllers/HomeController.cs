using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentResourceTracker.Data;
using StudentResourceTracker.Models;
using StudentResourceTracker.ViewModels;

namespace StudentResourceTracker.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(AppDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new DashboardViewModel
        {
            TotalStudents    = await _context.Students.CountAsync(),
            TotalResources   = await _context.Resources.CountAsync(),
            CompletedResources = await _context.Resources.CountAsync(r => r.IsCompleted),
            TotalCategories  = await _context.Categories.CountAsync(),
            RecentResources  = await _context.Resources
                                    .Include(r => r.Student)
                                    .Include(r => r.Category)
                                    .OrderByDescending(r => r.AddedOn)
                                    .Take(5)
                                    .ToListAsync(),
            RecentStudents   = await _context.Students
                                    .OrderByDescending(s => s.EnrolledOn)
                                    .Take(5)
                                    .ToListAsync()
        };

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
