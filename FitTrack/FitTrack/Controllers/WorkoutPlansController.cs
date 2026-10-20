
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitTrack.Models;
using FitTrack.Data;

public class WorkoutPlansController : Controller
{
    private readonly AppDbContext _context;

    public WorkoutPlansController(AppDbContext context)
    {
        _context = context;
    }

    // GET: WORKOUTPLANS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.WorkoutPlans.ToListAsync());
    }

    // GET: WORKOUTPLANS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var workoutplan = await _context.WorkoutPlans
            .FirstOrDefaultAsync(m => m.Id == id);
        if (workoutplan == null)
        {
            return NotFound();
        }

        return View(workoutplan);
    }

    // GET: WORKOUTPLANS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: WORKOUTPLANS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,UserProfileId,UserProfile,CreatedAt,Goal,IsActive,PlanExercises")] WorkoutPlan workoutplan)
    {
        if (ModelState.IsValid)
        {
            _context.Add(workoutplan);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(workoutplan);
    }

    // GET: WORKOUTPLANS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var workoutplan = await _context.WorkoutPlans.FindAsync(id);
        if (workoutplan == null)
        {
            return NotFound();
        }
        return View(workoutplan);
    }

    // POST: WORKOUTPLANS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,UserProfileId,UserProfile,CreatedAt,Goal,IsActive,PlanExercises")] WorkoutPlan workoutplan)
    {
        if (id != workoutplan.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(workoutplan);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WorkoutPlanExists(workoutplan.Id))
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
        return View(workoutplan);
    }

    // GET: WORKOUTPLANS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var workoutplan = await _context.WorkoutPlans
            .FirstOrDefaultAsync(m => m.Id == id);
        if (workoutplan == null)
        {
            return NotFound();
        }

        return View(workoutplan);
    }

    // POST: WORKOUTPLANS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var workoutplan = await _context.WorkoutPlans.FindAsync(id);
        if (workoutplan != null)
        {
            _context.WorkoutPlans.Remove(workoutplan);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool WorkoutPlanExists(int? id)
    {
        return _context.WorkoutPlans.Any(e => e.Id == id);
    }
}
