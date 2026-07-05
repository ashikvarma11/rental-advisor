namespace RentalAdvisor.Backend.Services;

public enum JobStatus { Unknown, Queued, Processing, Done, Failed }

public record JobStatusInfo(JobStatus Status, string? Error, int? ClausesCreated);
