using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_translator.Data;
using Project_translator.Models;

namespace Project_translator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SourceStringsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SourceStringsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/SourceStrings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SourceString>>> GetSourceStrings()
        {
            return await _context.SourceStrings
                .Include(s => s.Project)
                .ToListAsync();
        }

        // GET: api/SourceStrings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SourceString>> GetSourceString(int id)
        {
            var sourceString = await _context.SourceStrings
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sourceString == null)
            {
                return NotFound();
            }

            return sourceString;
        }

        // GET: api/SourceStrings/project/5
        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<SourceString>>> GetSourceStringsByProject(int projectId)
        {
            return await _context.SourceStrings
                .Where(s => s.ProjectId == projectId)
                .ToListAsync();
        }

        // POST: api/SourceStrings
        [HttpPost]
        public async Task<ActionResult<SourceString>> PostSourceString(SourceString sourceString)
        {
            _context.SourceStrings.Add(sourceString);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSourceString", new { id = sourceString.Id }, sourceString);
        }

        // PUT: api/SourceStrings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSourceString(int id, SourceString sourceString)
        {
            if (id != sourceString.Id)
            {
                return BadRequest();
            }

            _context.Entry(sourceString).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SourceStringExists(id))
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

        // DELETE: api/SourceStrings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSourceString(int id)
        {
            var sourceString = await _context.SourceStrings.FindAsync(id);
            if (sourceString == null)
            {
                return NotFound();
            }

            _context.SourceStrings.Remove(sourceString);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SourceStringExists(int id)
        {
            return _context.SourceStrings.Any(e => e.Id == id);
        }
    }
}
