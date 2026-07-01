namespace RentalAdvisor.Backend.Models;

public class SuburbStats
{
    public int Id { get; set; }
    public string Suburb { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public decimal MedianRent { get; set; }
    public int Year { get; set; }
}
