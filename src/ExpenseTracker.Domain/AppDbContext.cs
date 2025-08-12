using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Domain;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Expense>(e =>
        {
            e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            e.Property(p => p.UserObjectId).IsRequired().HasMaxLength(64);
            e.Property(p => p.Category).HasMaxLength(64);
        });
    }
}
