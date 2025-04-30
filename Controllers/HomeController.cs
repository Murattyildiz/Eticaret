using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Eticaret.Models;
using Microsoft.EntityFrameworkCore;

namespace Eticaret.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var featuredProducts = await _context.Products
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedDate)
            .Take(6)
            .ToListAsync();

        return View(featuredProducts);
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
