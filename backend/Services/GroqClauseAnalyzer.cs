using System.Text;
using System.Text.Json;

namespace RentalAdvisor.Backend.Services;

public record ClauseAnalysis(decimal RiskScore, string? Suggestion);
public record ExtractedClause(string Text, decimal RiskScore, string? Suggestion);
public record LeaseSummary(decimal? Rent, string? Suburb, string? Postcode);

// Calls Groq's OpenAI-compatible chat completions API (free tier) to score lease clause risk.
// Returns null when no API key is configured or the call fails, so callers can fall back to rule-based scoring.
public class GroqClauseAnalyzer
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<GroqClauseAnalyzer> _logger;

    public GroqClauseAnalyzer(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<GroqClauseAnalyzer> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_config["Groq:ApiKey"]);

    public async Task<ClauseAnalysis?> AnalyzeAsync(string clauseText, CancellationToken ct = default)
    {
        var apiKey = _config["Groq:ApiKey"];
        if (string.IsNullOrEmpty(apiKey)) return null;

        var model = _config["Groq:Model"] ?? "llama-3.3-70b-versatile";

        var systemPrompt = "You are a rental lease clause risk analyzer for Australian residential leases. " +
            "Given a single clause, respond with ONLY a JSON object: {\"riskScore\": <0.0-1.0>, \"suggestion\": \"<short advice or null>\"}. " +
            "riskScore reflects how risky/unusual the clause is for a tenant (0 = standard/safe, 1 = highly risky). " +
            "suggestion should be null unless riskScore > 0.5, in which case give one short actionable sentence.";

        var payload = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = clauseText }
            },
            temperature = 0.2,
            max_tokens = 150
        };

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(content)) return null;

            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd < jsonStart) return null;

            using var resultDoc = JsonDocument.Parse(content.Substring(jsonStart, jsonEnd - jsonStart + 1));
            var root = resultDoc.RootElement;
            var riskScore = root.TryGetProperty("riskScore", out var riskEl) ? riskEl.GetDecimal() : 0m;
            riskScore = Math.Clamp(riskScore, 0m, 1m);
            var suggestion = root.TryGetProperty("suggestion", out var suggEl) && suggEl.ValueKind == JsonValueKind.String
                ? suggEl.GetString()
                : null;

            return new ClauseAnalysis(riskScore, suggestion);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Groq clause analysis failed; falling back to rule-based scoring");
            return null;
        }
    }

    // Sends the full lease document text to Groq and lets the model both split it into clauses
    // and score/suggest each one, instead of the backend regex-splitting first. Retries once.
    public async Task<List<ExtractedClause>?> ExtractClausesAsync(string documentText, CancellationToken ct = default)
    {
        return await ExtractClausesAttemptAsync(documentText, ct)
            ?? await ExtractClausesAttemptAsync(documentText, ct);
    }

    private async Task<List<ExtractedClause>?> ExtractClausesAttemptAsync(string documentText, CancellationToken ct)
    {
        var apiKey = _config["Groq:ApiKey"];
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrWhiteSpace(documentText)) return null;

        var model = _config["Groq:Model"] ?? "llama-3.3-70b-versatile";

        var systemPrompt = "You are a rental lease clause extractor and risk analyzer for Australian residential leases. " +
            "You will receive the raw extracted text of a lease document (line breaks may not reflect original paragraphs). " +
            "Identify each distinct clause/term of the agreement (skip signature blocks, blank form fields with no content, and page headers/footers). " +
            "Respond with ONLY a JSON array, each item {\"text\": \"<verbatim or lightly cleaned clause text>\", \"riskScore\": <0.0-1.0>, \"suggestion\": \"<short advice or null>\"}. " +
            "riskScore reflects how risky/unusual the clause is for a tenant (0 = standard/safe, 1 = highly risky). " +
            "suggestion should be null unless riskScore > 0.5, in which case give one short actionable sentence. " +
            "Return at most 50 clauses, in document order.";

        var payload = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = documentText }
            },
            temperature = 0.2,
            max_tokens = 4096
        };

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(content)) return null;

            var jsonStart = content.IndexOf('[');
            var jsonEnd = content.LastIndexOf(']');
            if (jsonStart < 0 || jsonEnd < jsonStart) return null;

            using var resultDoc = JsonDocument.Parse(content.Substring(jsonStart, jsonEnd - jsonStart + 1));
            var results = new List<ExtractedClause>();
            foreach (var item in resultDoc.RootElement.EnumerateArray())
            {
                var text = item.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String
                    ? textEl.GetString()?.Trim()
                    : null;
                if (string.IsNullOrWhiteSpace(text)) continue;

                var riskScore = item.TryGetProperty("riskScore", out var riskEl) ? riskEl.GetDecimal() : 0m;
                riskScore = Math.Clamp(riskScore, 0m, 1m);
                var suggestion = item.TryGetProperty("suggestion", out var suggEl) && suggEl.ValueKind == JsonValueKind.String
                    ? suggEl.GetString()
                    : null;
                results.Add(new ExtractedClause(text!, riskScore, suggestion));
            }

            return results.Count > 0 ? results : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Groq clause extraction failed; falling back to regex-based splitting");
            return null;
        }
    }

    // Pulls weekly/monthly rent, suburb and postcode out of the raw lease text so extracted leases can feed the comparator's Listings table.
    public async Task<LeaseSummary?> ExtractLeaseSummaryAsync(string documentText, CancellationToken ct = default)
    {
        var apiKey = _config["Groq:ApiKey"];
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrWhiteSpace(documentText)) return null;

        var model = _config["Groq:Model"] ?? "llama-3.3-70b-versatile";

        var systemPrompt = "You are an information extractor for Australian residential lease documents. " +
            "From the raw lease text, find the rent property's suburb, 4-digit postcode, and rent amount converted to AUD per week. " +
            "Respond with ONLY a JSON object: {\"rent\": <weekly rent number or null>, \"suburb\": \"<suburb or null>\", \"postcode\": \"<4-digit postcode or null>\"}. " +
            "Use null for any field you cannot find with confidence. Convert monthly/fortnightly rent to a weekly figure.";

        var payload = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = documentText }
            },
            temperature = 0.1,
            max_tokens = 200
        };

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(content)) return null;

            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd < jsonStart) return null;

            using var resultDoc = JsonDocument.Parse(content.Substring(jsonStart, jsonEnd - jsonStart + 1));
            var root = resultDoc.RootElement;

            decimal? rent = root.TryGetProperty("rent", out var rentEl) && rentEl.ValueKind == JsonValueKind.Number
                ? rentEl.GetDecimal()
                : null;
            var suburb = root.TryGetProperty("suburb", out var suburbEl) && suburbEl.ValueKind == JsonValueKind.String
                ? suburbEl.GetString()?.Trim()
                : null;
            var postcode = root.TryGetProperty("postcode", out var postcodeEl) && postcodeEl.ValueKind == JsonValueKind.String
                ? postcodeEl.GetString()?.Trim()
                : null;

            return new LeaseSummary(rent, suburb, postcode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Groq lease summary extraction failed");
            return null;
        }
    }

    // Scores all clauses in a single Groq call, retrying once if the model drops/merges items. Returns null (caller falls back to rule-based scoring) if both attempts fail.
    public async Task<List<ClauseAnalysis>?> AnalyzeBatchAsync(List<string> clauseTexts, CancellationToken ct = default)
    {
        return await AnalyzeBatchAttemptAsync(clauseTexts, ct)
            ?? await AnalyzeBatchAttemptAsync(clauseTexts, ct);
    }

    private async Task<List<ClauseAnalysis>?> AnalyzeBatchAttemptAsync(List<string> clauseTexts, CancellationToken ct)
    {
        var apiKey = _config["Groq:ApiKey"];
        if (string.IsNullOrEmpty(apiKey) || clauseTexts.Count == 0) return null;

        var model = _config["Groq:Model"] ?? "llama-3.3-70b-versatile";

        var systemPrompt = "You are a rental lease clause risk analyzer for Australian residential leases. " +
            $"You will receive a JSON array of exactly {clauseTexts.Count} clause texts, each prefixed with its index like \"[0] text\". " +
            $"Respond with ONLY a JSON array of exactly {clauseTexts.Count} items, one per input item in the same order - never merge, skip, or split items. " +
            "each item {\"riskScore\": <0.0-1.0>, \"suggestion\": \"<short advice or null>\"}. " +
            "riskScore reflects how risky/unusual the clause is for a tenant (0 = standard/safe, 1 = highly risky). " +
            "suggestion should be null unless riskScore > 0.5, in which case give one short actionable sentence.";

        var indexedClauses = clauseTexts.Select((c, i) => $"[{i}] {c}").ToList();

        var payload = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = JsonSerializer.Serialize(indexedClauses) }
            },
            temperature = 0.2,
            max_tokens = 150 * clauseTexts.Count
        };

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(content)) return null;

            var jsonStart = content.IndexOf('[');
            var jsonEnd = content.LastIndexOf(']');
            if (jsonStart < 0 || jsonEnd < jsonStart) return null;

            using var resultDoc = JsonDocument.Parse(content.Substring(jsonStart, jsonEnd - jsonStart + 1));
            var results = new List<ClauseAnalysis>();
            foreach (var item in resultDoc.RootElement.EnumerateArray())
            {
                var riskScore = item.TryGetProperty("riskScore", out var riskEl) ? riskEl.GetDecimal() : 0m;
                riskScore = Math.Clamp(riskScore, 0m, 1m);
                var suggestion = item.TryGetProperty("suggestion", out var suggEl) && suggEl.ValueKind == JsonValueKind.String
                    ? suggEl.GetString()
                    : null;
                results.Add(new ClauseAnalysis(riskScore, suggestion));
            }

            if (results.Count != clauseTexts.Count)
            {
                _logger.LogWarning("Groq batch clause analysis returned {Actual} items, expected {Expected}; falling back to rule-based scoring", results.Count, clauseTexts.Count);
                return null;
            }
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Groq batch clause analysis failed; falling back to rule-based scoring");
            return null;
        }
    }
}
