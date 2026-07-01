using Microsoft.EntityFrameworkCore;
using RentalAdvisor.Backend.Models;

namespace RentalAdvisor.Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Listing> Listings { get; set; } = null!;
    public DbSet<SuburbStats> SuburbStats { get; set; } = null!;
    public DbSet<LeaseDocument> LeaseDocuments { get; set; } = null!;
    public DbSet<Clause> Clauses { get; set; } = null!;
}
