using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_translator.Data;
using Project_translator.Models;

namespace Project_translator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GlossaryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GlossaryController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GlossaryTerm>>> GetGlossaryTerms([FromQuery] int? projectId = null)
        {
            var query = _context.GlossaryTerms.Include(g => g.Project).AsQueryable();

            if (projectId.HasValue)
            {
                query = query.Where(g => g.ProjectId == projectId.Value);
            }

            return await query.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GlossaryTerm>> GetGlossaryTerm(int id)
        {
            var term = await _context.GlossaryTerms
                .Include(g => g.Project)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (term == null)
            {
                return NotFound();
            }

            return term;
        }

        [HttpPost]
        public async Task<ActionResult<GlossaryTerm>> PostGlossaryTerm(GlossaryTerm term)
        {
            _context.GlossaryTerms.Add(term);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetGlossaryTerm", new { id = term.Id }, term);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutGlossaryTerm(int id, GlossaryTerm term)
        {
            if (id != term.Id)
            {
                return BadRequest();
            }

            _context.Entry(term).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GlossaryTermExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGlossaryTerm(int id)
        {
            var term = await _context.GlossaryTerms.FindAsync(id);
            if (term == null)
            {
                return NotFound();
            }

            _context.GlossaryTerms.Remove(term);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool GlossaryTermExists(int id)
        {
            return _context.GlossaryTerms.Any(e => e.Id == id);
        }
    }
}