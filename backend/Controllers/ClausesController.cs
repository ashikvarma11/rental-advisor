using Microsoft.AspNetCore.Mvc;
using RentalAdvisor.Backend.Data;
using RentalAdvisor.Backend.Models;

namespace RentalAdvisor.Backend.Controllers;

[ApiController]
[Route("api/clauses")]
public class ClausesController : ControllerBase
{
    private readonly AppDbContext _db;
    public ClausesController(AppDbContext db) { _db = db; }

    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> Resolve(int id)
    {
        var clause = await _db.Clauses.FindAsync(id);
        if (clause == null) return NotFound();
        clause.IsResolved = true;
        await _db.SaveChangesAsync();
        return Ok(new { id = clause.Id, isResolved = clause.IsResolved });
    }
}
