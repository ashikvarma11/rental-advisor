using System;

namespace RentalAdvisor.Backend.Models;

public class LeaseDocument
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? Content { get; set; }
}
