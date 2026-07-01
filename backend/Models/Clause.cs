namespace RentalAdvisor.Backend.Models;

public class Clause
{
    public int Id { get; set; }
    public int LeaseDocumentId { get; set; }
    public string Text { get; set; } = string.Empty;
    public decimal RiskScore { get; set; }
    public string? Suggestion { get; set; }
    public bool IsResolved { get; set; } = false;
}
