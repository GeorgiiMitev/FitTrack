
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitTrack.Models;
using FitTrack.Data;

public class ProgressEntriesController : Controller
{
    private readonly AppDbContext _context;

    public ProgressEntriesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: PROGRESSENTRYS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.ProgressEntries.ToListAsync());
    }

    // GET: PROGRESSENTRYS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var progressentry = await _context.ProgressEntries
            .FirstOrDefaultAsync(m => m.Id == id);
        if (progressentry == null)
        {
            return NotFound();
        }

        return View(progressentry);
    }

    // GET: PROGRESSENTRYS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: PROGRESSENTRYS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,UserProfileId,UserProfile,MaxWeight,Date")] ProgressEntry progressentry)
    {
        if (ModelState.IsValid)
        {
            _context.Add(progressentry);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(progressentry);
    }

    // GET: PROGRESSENTRYS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var progressentry = await _context.ProgressEntries.FindAsync(id);
        if (progressentry == null)
        {
            return NotFound();
        }
        return View(progressentry);
    }

    // POST: PROGRESSENTRYS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,UserProfileId,UserProfile,MaxWeight,Date")] ProgressEntry progressentry)
    {
        if (id != progressentry.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(progressentry);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProgressEntryExists(progressentry.Id))
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
        return View(progressentry);
    }

    // GET: PROGRESSENTRYS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var progressentry = await _context.ProgressEntries
            .FirstOrDefaultAsync(m => m.Id == id);
        if (progressentry == null)
        {
            return NotFound();
        }

        return View(progressentry);
    }

    // POST: PROGRESSENTRYS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var progressentry = await _context.ProgressEntries.FindAsync(id);
        if (progressentry != null)
        {
            _context.ProgressEntries.Remove(progressentry);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ProgressEntryExists(int? id)
    {
        return _context.ProgressEntries.Any(e => e.Id == id);
    }
}
