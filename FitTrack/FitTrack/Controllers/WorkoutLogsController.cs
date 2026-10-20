
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitTrack.Models;
using FitTrack.Data;

public class WorkoutLogsController : Controller
{
    private readonly AppDbContext _context;

    public WorkoutLogsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: WORKOUTLOGS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.WorkoutLogs.ToListAsync());
    }

    // GET: WORKOUTLOGS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var workoutlog = await _context.WorkoutLogs
            .FirstOrDefaultAsync(m => m.Id == id);
        if (workoutlog == null)
        {
            return NotFound();
        }

        return View(workoutlog);
    }

    // GET: WORKOUTLOGS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: WORKOUTLOGS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,ExerciseName,UserProfileId,UserProfile,CompletedAt,SetsCompleted,RepetitionsCompleted,WeightUsed,Notes")] WorkoutLog workoutlog)
    {
        if (ModelState.IsValid)
        {
            _context.Add(workoutlog);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(workoutlog);
    }

    // GET: WORKOUTLOGS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var workoutlog = await _context.WorkoutLogs.FindAsync(id);
        if (workoutlog == null)
        {
            return NotFound();
        }
        return View(workoutlog);
    }

    // POST: WORKOUTLOGS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,ExerciseName,UserProfileId,UserProfile,CompletedAt,SetsCompleted,RepetitionsCompleted,WeightUsed,Notes")] WorkoutLog workoutlog)
    {
        if (id != workoutlog.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(workoutlog);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WorkoutLogExists(workoutlog.Id))
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
        return View(workoutlog);
    }

    // GET: WORKOUTLOGS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var workoutlog = await _context.WorkoutLogs
            .FirstOrDefaultAsync(m => m.Id == id);
        if (workoutlog == null)
        {
            return NotFound();
        }

        return View(workoutlog);
    }

    // POST: WORKOUTLOGS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var workoutlog = await _context.WorkoutLogs.FindAsync(id);
        if (workoutlog != null)
        {
            _context.WorkoutLogs.Remove(workoutlog);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool WorkoutLogExists(int? id)
    {
        return _context.WorkoutLogs.Any(e => e.Id == id);
    }
}
