using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalAdvisor.Backend.Data;
using RentalAdvisor.Backend.Models;

namespace RentalAdvisor.Backend.Controllers;

[ApiController]
[Route("api/clauses")]
[Authorize]
public class ClausesController : ControllerBase
{
    private readonly AppDbContext _db;
    public ClausesController(AppDbContext db) { _db = db; }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> Resolve(int id)
    {
        var clause = await _db.Clauses.Include(c => c.LeaseDocument)
            .FirstOrDefaultAsync(c => c.Id == id && c.LeaseDocument!.UserId == UserId);
        if (clause == null) return NotFound();
        clause.IsResolved = true;
        await _db.SaveChangesAsync();
        return Ok(new { id = clause.Id, isResolved = clause.IsResolved });
    }
}
