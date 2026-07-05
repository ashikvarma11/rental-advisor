using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RentalAdvisor.Backend.Data;
using RentalAdvisor.Backend.Models;
using UglyToad.PdfPig;

namespace RentalAdvisor.Backend.Services;

public class LeaseDocumentNotFoundException : Exception { }

public class ClauseExtractionService
{
    private readonly AppDbContext _db;
    private readonly GroqClauseAnalyzer _aiAnalyzer;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ClauseExtractionService> _logger;

    public ClauseExtractionService(AppDbContext db, GroqClauseAnalyzer aiAnalyzer, IWebHostEnvironment env, ILogger<ClauseExtractionService> logger)
    {
        _db = db;
        _aiAnalyzer = aiAnalyzer;
        _env = env;
        _logger = logger;
    }

    public async Task<int> ExtractAndPersistAsync(int leaseDocumentId, int userId, CancellationToken ct = default)
    {
        var doc = await _db.LeaseDocuments.FirstOrDefaultAsync(d => d.Id == leaseDocumentId && d.UserId == userId, ct);
        if (doc == null) throw new LeaseDocumentNotFoundException();

        string? content = doc.Content;
        if (string.IsNullOrEmpty(content))
        {
            var path = Path.Combine(_env.ContentRootPath, "Data", "leases", doc.FileName);
            if (System.IO.File.Exists(path))
                content = await ExtractTextFromFileAsync(path);
        }

        if (string.IsNullOrEmpty(content))
            throw new InvalidOperationException("No text content available for clause extraction");

        // Prefer letting Groq split the raw text into clauses and score them in one pass -
        // it handles messy PDF-extracted layout far better than a fixed regex/blank-line split.
        var extracted = _aiAnalyzer.IsConfigured ? await _aiAnalyzer.ExtractClausesAsync(content) : null;

        var created = 0;
        if (extracted != null)
        {
            foreach (var e in extracted)
            {
                _db.Clauses.Add(new Clause
                {
                    LeaseDocumentId = doc.Id,
                    Text = e.Text,
                    RiskScore = e.RiskScore,
                    Suggestion = e.Suggestion
                });
                created++;
            }
        }
        else
        {
            var clauses = SplitIntoClauses(content);
            var batch = _aiAnalyzer.IsConfigured ? await _aiAnalyzer.AnalyzeBatchAsync(clauses) : null;

            for (var i = 0; i < clauses.Count; i++)
            {
                var c = clauses[i];
                var analysis = batch?[i];

                decimal score;
                string? suggestion;
                if (analysis != null)
                {
                    score = analysis.RiskScore;
                    suggestion = analysis.Suggestion;
                }
                else
                {
                    score = 0m;
                    var t = c.ToLowerInvariant();
                    if (t.Contains("terminate") || t.Contains("breach") || t.Contains("penalty")) score += 0.6m;
                    if (t.Contains("rent") || t.Contains("increase")) score += 0.2m;
                    if (t.Contains("bond") || t.Contains("deposit")) score += 0.2m;
                    score = Math.Min(1m, score);
                    suggestion = score > 0.5m ? "Review with attention - possible high-risk clause." : null;
                }

                _db.Clauses.Add(new Clause
                {
                    LeaseDocumentId = doc.Id,
                    Text = c,
                    RiskScore = score,
                    Suggestion = suggestion
                });
                created++;
            }
        }

        var summary = _aiAnalyzer.IsConfigured ? await _aiAnalyzer.ExtractLeaseSummaryAsync(content) : null;
        if (summary == null || summary.Rent is not > 0 || string.IsNullOrWhiteSpace(summary.Suburb) || string.IsNullOrWhiteSpace(summary.Postcode))
        {
            // Groq unavailable/rate-limited: fall back to regex against the fixed-format CBS lease template.
            summary = ExtractLeaseSummaryByRegex(content);
        }

        if (summary != null && summary.Rent > 0 && !string.IsNullOrWhiteSpace(summary.Suburb)
            && !string.IsNullOrWhiteSpace(summary.Postcode) && Regex.IsMatch(summary.Postcode, "^\\d{4}$"))
        {
            _db.Listings.Add(new Listing
            {
                UserId = userId,
                Title = $"Lease upload: {doc.FileName}",
                Suburb = summary.Suburb!,
                Postcode = summary.Postcode!,
                Rent = summary.Rent.Value
            });
        }

        await _db.SaveChangesAsync(ct);
        return created;
    }

    // PdfPig's page.Text has no blank-line paragraph breaks, so splitting on "\n\n" collapses
    // the whole document into one clause. Split on numbered clause headings (e.g. "12. Termination...")
    // instead, since that's the actual structural unit in these lease documents; fall back to
    // blank-line/line splitting for documents that aren't numbered.
    private static readonly Regex NumberedClauseHeading =
        new(@"(?m)^\s*(\d{1,2})\.\s+\S", RegexOptions.Compiled);

    private static List<string> SplitIntoClauses(string content)
    {
        var matches = NumberedClauseHeading.Matches(content).ToList();
        if (matches.Count >= 3)
        {
            var parts = new List<string>();
            for (var i = 0; i < matches.Count; i++)
            {
                var start = matches[i].Index;
                var end = i + 1 < matches.Count ? matches[i + 1].Index : content.Length;
                parts.Add(content.Substring(start, end - start).Trim());
            }
            return parts.Where(t => t.Length > 20).Take(50).ToList();
        }

        var byBlankLine = content.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => t.Length > 20)
            .ToList();
        if (byBlankLine.Count > 1)
            return byBlankLine.Take(50).ToList();

        return content.Split('\n')
            .Select(t => t.Trim())
            .Where(t => t.Length > 20)
            .Take(50)
            .ToList();
    }

    // Fallback for when Groq is unavailable: the CBS fixed-term lease template always has
    // "Address of premises: <street>, <suburb> SA <postcode>" and "Weekly amount: $<rent>".
    private static LeaseSummary? ExtractLeaseSummaryByRegex(string content)
    {
        var addressMatch = Regex.Match(content,
            @"Address of premises:\s*.*?,\s*([A-Za-z .'-]+?)\s+[A-Z]{2,3}\s+(\d{4})");
        var rentMatch = Regex.Match(content,
            @"Weekly amount:\s*\$?\s*([\d,]+(?:\.\d{1,2})?)");

        if (!addressMatch.Success || !rentMatch.Success) return null;

        var suburb = addressMatch.Groups[1].Value.Trim();
        var postcode = addressMatch.Groups[2].Value.Trim();
        if (!decimal.TryParse(rentMatch.Groups[1].Value.Replace(",", ""), out var rent)) return null;

        return new LeaseSummary(rent, suburb, postcode);
    }

    private async Task<string?> ExtractTextFromFileAsync(string path)
    {
        if (!System.IO.File.Exists(path)) return null;
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".txt")
        {
            return await System.IO.File.ReadAllTextAsync(path);
        }
        if (ext == ".pdf")
        {
            try
            {
                var sb = new StringBuilder();
                using (var pdf = PdfDocument.Open(path))
                {
                    foreach (var page in pdf.GetPages())
                    {
                        sb.AppendLine(page.Text);
                    }
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PDF text extraction failed for {Path}", path);
                return null;
            }
        }

        return null;
    }
}
