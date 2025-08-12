using System.Security.Claims;
using ExpenseTracker.Domain;
using ExpenseTracker.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Web.Controllers;

[Authorize]
public class ExpensesController : Controller
{
    private readonly AppDbContext _db;
    private readonly ReceiptStorage _storage;

    public ExpensesController(AppDbContext db, ReceiptStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    private string UserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("No OID");

    public class ExpenseFilters
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Category { get; set; }
    }
    private static IQueryable<Expense> ApplyFilters(IQueryable<Expense> q, ExpenseFilters f)
    {
        if (f.From.HasValue) q = q.Where(x => x.Date >= f.From.Value.Date);
        if (f.To.HasValue) q = q.Where(x => x.Date <= f.To.Value.Date.AddDays(1).AddTicks(-1));
        if (!string.IsNullOrWhiteSpace(f.Category) && f.Category != "All") q = q.Where(x => x.Category == f.Category);
        return q;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ExpenseFilters filters)
    {
        var uid = UserId();
        var categories = await _db.Expenses.Where(x => x.UserObjectId == uid)
            .Select(x => x.Category).Distinct().OrderBy(x => x).ToListAsync();

        var q = ApplyFilters(_db.Expenses.Where(x => x.UserObjectId == uid), filters);
        var items = await q.OrderByDescending(x => x.Date).ToListAsync();
        ViewBag.Total = items.Sum(x => x.Amount);
        ViewBag.Categories = categories;
        ViewBag.Filters = filters;
        return View(items);
    }

    [HttpGet] public IActionResult Create() => View();

    public class CreateExpenseVm
    {
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Category { get; set; } = "General";
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public IFormFile? Receipt { get; set; }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateExpenseVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        string? receiptUrl = null;
        if (vm.Receipt is { Length: > 0 })
        {
            using var s = vm.Receipt.OpenReadStream();
            receiptUrl = await _storage.UploadAsync(s, vm.Receipt.FileName, vm.Receipt.ContentType, HttpContext.RequestAborted);
        }
        _db.Expenses.Add(new Expense
        {
            UserObjectId = UserId(),
            Amount = vm.Amount,
            Category = vm.Category,
            Date = vm.Date,
            Description = vm.Description,
            ReceiptBlobUrl = receiptUrl
        });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var uid = UserId();
        var e = await _db.Expenses.FirstOrDefaultAsync(x => x.Id == id && x.UserObjectId == uid);
        if (e != null) { _db.Expenses.Remove(e); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/expenses/chart-data")]
    public async Task<IActionResult> ChartData([FromQuery] ExpenseFilters filters)
    {
        var uid = UserId();
        var q = ApplyFilters(_db.Expenses.Where(x => x.UserObjectId == uid), filters);

        var now = DateTime.UtcNow.Date;
        var start90 = filters.From ?? now.AddDays(-89);
        var end90 = (filters.To ?? now).Date.AddDays(1).AddTicks(-1);

        var byCategory = await q.Where(x => x.Date >= start90 && x.Date <= end90)
            .GroupBy(x => x.Category)
            .Select(g => new { label = g.Key, amount = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.amount).ToListAsync();

        var start6m = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
        var rangeStart = filters.From ?? start6m;
        var rangeEnd = (filters.To ?? now).Date.AddDays(1).AddTicks(-1);
        var byMonth = await q.Where(x => x.Date >= rangeStart && x.Date <= rangeEnd)
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(g => new { label = $"{g.Key.Year}-{g.Key.Month:00}", amount = g.Sum(x => x.Amount) })
            .OrderBy(x => x.label).ToListAsync();

        return Json(new { byCategory, byMonth });
    }
}
