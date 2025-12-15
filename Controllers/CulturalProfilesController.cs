using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_translator.Data;
using Project_translator.Models;

namespace Project_translator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CulturalProfilesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CulturalProfilesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/CulturalProfiles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CulturalProfile>>> GetCulturalProfiles()
        {
            return await _context.CulturalProfiles
                .Include(c => c.Locale)
                .ToListAsync();
        }

        // GET: api/CulturalProfiles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CulturalProfile>> GetCulturalProfile(int id)
        {
            var culturalProfile = await _context.CulturalProfiles
                .Include(c => c.Locale)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (culturalProfile == null)
            {
                return NotFound();
            }

            return culturalProfile;
        }

        // GET: api/CulturalProfiles/locale/2
        [HttpGet("locale/{localeId}")]
        public async Task<ActionResult<CulturalProfile>> GetCulturalProfileByLocale(int localeId)
        {
            var culturalProfile = await _context.CulturalProfiles
                .Include(c => c.Locale)
                .FirstOrDefaultAsync(c => c.LocaleId == localeId);

            if (culturalProfile == null)
            {
                return NotFound($"Cultural profile for locale ID {localeId} not found");
            }

            return culturalProfile;
        }

        // GET: api/CulturalProfiles/code/ru
        [HttpGet("code/{localeCode}")]
        public async Task<ActionResult<CulturalProfile>> GetCulturalProfileByCode(string localeCode)
        {
            var culturalProfile = await _context.CulturalProfiles
                .Include(c => c.Locale)
                .FirstOrDefaultAsync(c => c.Locale.Code == localeCode);

            if (culturalProfile == null)
            {
                return NotFound($"Cultural profile for locale code '{localeCode}' not found");
            }

            return culturalProfile;
        }

        // PUT: api/CulturalProfiles/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCulturalProfile(int id, CulturalProfile culturalProfile)
        {
            if (id != culturalProfile.Id)
            {
                return BadRequest();
            }

            // Проверяем, существует ли локаль
            var localeExists = await _context.Locales
                .AnyAsync(l => l.Id == culturalProfile.LocaleId);

            if (!localeExists)
            {
                return BadRequest($"Locale with ID {culturalProfile.LocaleId} does not exist");
            }

            _context.Entry(culturalProfile).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CulturalProfileExists(id))
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

        // POST: api/CulturalProfiles
        [HttpPost]
        public async Task<ActionResult<CulturalProfile>> PostCulturalProfile(CulturalProfile culturalProfile)
        {
            // Проверяем, существует ли локаль
            var localeExists = await _context.Locales
                .AnyAsync(l => l.Id == culturalProfile.LocaleId);

            if (!localeExists)
            {
                return BadRequest($"Locale with ID {culturalProfile.LocaleId} does not exist");
            }

            // Проверяем, нет ли уже профиля для этой локали
            var existingProfile = await _context.CulturalProfiles
                .FirstOrDefaultAsync(c => c.LocaleId == culturalProfile.LocaleId);

            if (existingProfile != null)
            {
                return Conflict($"Cultural profile for locale ID {culturalProfile.LocaleId} already exists");
            }

            _context.CulturalProfiles.Add(culturalProfile);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCulturalProfile", new { id = culturalProfile.Id }, culturalProfile);
        }

        // DELETE: api/CulturalProfiles/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCulturalProfile(int id)
        {
            var culturalProfile = await _context.CulturalProfiles.FindAsync(id);
            if (culturalProfile == null)
            {
                return NotFound();
            }

            _context.CulturalProfiles.Remove(culturalProfile);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/CulturalProfiles/5/currency
        [HttpPatch("{id}/currency")]
        public async Task<IActionResult> UpdateCurrency(int id, [FromBody] string currency)
        {
            var culturalProfile = await _context.CulturalProfiles.FindAsync(id);
            if (culturalProfile == null)
            {
                return NotFound();
            }

            culturalProfile.Currency = currency;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/CulturalProfiles/5/date-format
        [HttpPatch("{id}/date-format")]
        public async Task<IActionResult> UpdateDateFormat(int id, [FromBody] string dateFormat)
        {
            var culturalProfile = await _context.CulturalProfiles.FindAsync(id);
            if (culturalProfile == null)
            {
                return NotFound();
            }

            culturalProfile.DateFormat = dateFormat;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CulturalProfileExists(int id)
        {
            return _context.CulturalProfiles.Any(e => e.Id == id);
        }
    }
}