using System;

namespace RentalAdvisor.Backend.Models;

public class Listing
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Suburb { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public decimal Rent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? LandlordAbn { get; set; }
    public int? SuburbStatsId { get; set; }
    public SuburbStats? SuburbStats { get; set; }
}
