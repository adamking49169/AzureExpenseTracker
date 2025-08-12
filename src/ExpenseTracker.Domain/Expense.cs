using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTracker.Domain;

public class Expense
{
    public int Id { get; set; }
    public string UserObjectId { get; set; } = default!;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Category { get; set; } = "General";
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ReceiptBlobUrl { get; set; }
}

