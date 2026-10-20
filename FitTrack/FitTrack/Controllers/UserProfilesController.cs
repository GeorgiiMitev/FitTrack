
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitTrack.Models;
using FitTrack.Data;

public class UserProfilesController : Controller
{
    private readonly AppDbContext _context;

    public UserProfilesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: USERPROFILES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.UserProfiles.ToListAsync());
    }

    // GET: USERPROFILES/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userprofile = await _context.UserProfiles
            .FirstOrDefaultAsync(m => m.Id == id);
        if (userprofile == null)
        {
            return NotFound();
        }

        return View(userprofile);
    }

    // GET: USERPROFILES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: USERPROFILES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,Goal,TrainingDaysPerWeek,SessionDurationMinutes,Equipment,WorkoutPlans,WorkoutLogs,ProgressEntries")] UserProfile userprofile)
    {
        if (ModelState.IsValid)
        {
            _context.Add(userprofile);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(userprofile);
    }

    // GET: USERPROFILES/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userprofile = await _context.UserProfiles.FindAsync(id);
        if (userprofile == null)
        {
            return NotFound();
        }
        return View(userprofile);
    }

    // POST: USERPROFILES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,Name,Goal,TrainingDaysPerWeek,SessionDurationMinutes,Equipment,WorkoutPlans,WorkoutLogs,ProgressEntries")] UserProfile userprofile)
    {
        if (id != userprofile.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(userprofile);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserProfileExists(userprofile.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(userprofile);
    }

    // GET: USERPROFILES/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userprofile = await _context.UserProfiles
            .FirstOrDefaultAsync(m => m.Id == id);
        if (userprofile == null)
        {
            return NotFound();
        }

        return View(userprofile);
    }

    // POST: USERPROFILES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var userprofile = await _context.UserProfiles.FindAsync(id);
        if (userprofile != null)
        {
            _context.UserProfiles.Remove(userprofile);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool UserProfileExists(int? id)
    {
        return _context.UserProfiles.Any(e => e.Id == id);
    }
}
