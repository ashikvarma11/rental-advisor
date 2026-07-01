using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace RentalAdvisor.Backend.Controllers;

[ApiController]
[Route("api/enrich/abn")]
public class EnrichController : ControllerBase
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EnrichController> _logger;

    public EnrichController(IMemoryCache cache, IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<EnrichController> logger)
    {
        _cache = cache;
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("{abn}")]
    public async Task<ActionResult> Get(string abn)
    {
        var normalizedAbn = new string(abn.Where(char.IsDigit).ToArray());
        if (normalizedAbn.Length != 11)
            return BadRequest(new { error = "ABN must be 11 digits" });

        var key = $"abr_{normalizedAbn}";
        if (_cache.TryGetValue(key, out var cached))
            return Ok(cached);

        var apiKey = _config["ABR:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            return BadRequest(new { error = "ABR API key not configured on backend" });

        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://abr.business.gov.au/json/AbnDetails.aspx?abn={normalizedAbn}&guid={apiKey}";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();

            // The ABR service wraps its JSON payload in a callback(...) function; strip it if present.
            var jsonStart = body.IndexOf('{');
            var jsonEnd = body.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd < jsonStart)
                throw new JsonException("Unexpected ABR response format");
            var json = body.Substring(jsonStart, jsonEnd - jsonStart + 1);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("Message", out var messageEl) && !string.IsNullOrEmpty(messageEl.GetString()))
                return NotFound(new { error = messageEl.GetString() });

            var result = new
            {
                abn = normalizedAbn,
                name = root.TryGetProperty("EntityName", out var nameEl) ? nameEl.GetString() : null,
                entityType = root.TryGetProperty("EntityTypeName", out var typeEl) ? typeEl.GetString() : null,
                status = root.TryGetProperty("AbnStatus", out var statusEl) ? statusEl.GetString() : null,
                mainBusinessLocation = root.TryGetProperty("AddressState", out var stateEl) ? stateEl.GetString() : null,
                postcode = root.TryGetProperty("AddressPostcode", out var postcodeEl) ? postcodeEl.GetString() : null,
                gstRegistered = root.TryGetProperty("Gst", out var gstEl) && !string.IsNullOrEmpty(gstEl.GetString()),
                lastUpdated = DateTime.UtcNow
            };

            _cache.Set(key, result, TimeSpan.FromHours(24));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ABR lookup failed for ABN {Abn}", normalizedAbn);
            return StatusCode(502, new { error = "ABR lookup failed", detail = ex.Message });
        }
    }
}
