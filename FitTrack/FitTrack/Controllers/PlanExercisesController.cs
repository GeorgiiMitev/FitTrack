
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitTrack.Models;
using FitTrack.Data;

public class PlanExercisesController : Controller
{
    private readonly AppDbContext _context;

    public PlanExercisesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: PLANEXERCISES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.PlanExercises.ToListAsync());
    }

    // GET: PLANEXERCISES/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var planexercise = await _context.PlanExercises
            .FirstOrDefaultAsync(m => m.Id == id);
        if (planexercise == null)
        {
            return NotFound();
        }

        return View(planexercise);
    }

    // GET: PLANEXERCISES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: PLANEXERCISES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,ExerciseName,WorkoutPlanId,WorkoutPlan,DayOfTheWeek,Sets,Repetitions,RestSeconds")] PlanExercise planexercise)
    {
        if (ModelState.IsValid)
        {
            _context.Add(planexercise);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(planexercise);
    }

    // GET: PLANEXERCISES/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var planexercise = await _context.PlanExercises.FindAsync(id);
        if (planexercise == null)
        {
            return NotFound();
        }
        return View(planexercise);
    }

    // POST: PLANEXERCISES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,ExerciseName,WorkoutPlanId,WorkoutPlan,DayOfTheWeek,Sets,Repetitions,RestSeconds")] PlanExercise planexercise)
    {
        if (id != planexercise.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(planexercise);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlanExerciseExists(planexercise.Id))
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
        return View(planexercise);
    }

    // GET: PLANEXERCISES/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var planexercise = await _context.PlanExercises
            .FirstOrDefaultAsync(m => m.Id == id);
        if (planexercise == null)
        {
            return NotFound();
        }

        return View(planexercise);
    }

    // POST: PLANEXERCISES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var planexercise = await _context.PlanExercises.FindAsync(id);
        if (planexercise != null)
        {
            _context.PlanExercises.Remove(planexercise);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool PlanExerciseExists(int? id)
    {
        return _context.PlanExercises.Any(e => e.Id == id);
    }
}
