using System.Security.Claims;
using FitTrack.Data;
using FitTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Controllers;

public sealed class UserProfilesController(AppDbContext db, IWebHostEnvironment environment) : Controller
{
    private static readonly string[] Goals = ["General fitness", "Muscle building", "Weight loss", "Strength", "Endurance", "Mobility"];
    private static readonly string[] Weekdays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
    private static readonly IReadOnlyDictionary<string, string[]> EquipmentGroups = new Dictionary<string, string[]>
    {
        ["Free weights"] = ["Dumbbells", "Barbell", "EZ Curl Bar", "Kettlebells", "Weight Plates", "Medicine Ball", "Trap Bar"],
        ["Machines"] = ["Smith Machine", "Cable Machine", "Leg Press Machine", "Lat Pulldown Machine", "Chest Press Machine", "Leg Extension Machine", "Leg Curl Machine", "Pec Deck Machine", "Assisted Pull-Up Machine"],
        ["Bodyweight & resistance"] = ["Bodyweight", "Pull-Up Bar", "Dip Bars", "Parallel Bars", "Gymnastic Rings", "Push-Up Bars", "Resistance Bands", "Resistance Tubes", "Suspension Trainer", "Weighted Vest"],
        ["Benches & racks"] = ["Flat Bench", "Adjustable Bench", "Squat Rack", "Power Rack", "Preacher Curl Bench"],
        ["Cardio equipment"] = ["Treadmill", "Exercise Bike", "Rowing Machine", "Elliptical Machine"],
        ["Accessories"] = ["Jump Rope", "Ab Wheel", "Exercise Mat", "Foam Roller"]
    };
    private static readonly string[] EquipmentOptions = EquipmentGroups.Values.SelectMany(items => items).ToArray();

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var profile = await GetProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        if (!profile.SetupCompleted) return RedirectToAction(nameof(Setup));
        return View(ToViewModel(profile));
    }

    [HttpGet]
    public async Task<IActionResult> Setup(CancellationToken cancellationToken)
    {
        var profile = await GetProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        if (profile.SetupCompleted) return RedirectToAction(nameof(Index));
        PopulateChoices();
        return View(ToViewModel(profile));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Setup(ProfileEditViewModel model, CancellationToken cancellationToken)
    {
        var profile = await GetProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        if (!ApplyValidatedProfile(model, profile))
        {
            PopulateChoices();
            return View(model);
        }
        profile.SetupCompleted = true;
        await SavePhotoAsync(model.ProfilePhoto, profile, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(CancellationToken cancellationToken)
    {
        var profile = await GetProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        PopulateChoices();
        return View(ToViewModel(profile));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileEditViewModel model, CancellationToken cancellationToken)
    {
        var profile = await GetProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        if (!ApplyValidatedProfile(model, profile))
        {
            model.CurrentPhotoPath = profile.ProfilePhotoPath;
            PopulateChoices();
            return View(model);
        }
        await SavePhotoAsync(model.ProfilePhoto, profile, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        TempData["ProfileSaved"] = "Profile updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<UserProfile?> GetProfileAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await db.UserProfiles.FirstOrDefaultAsync(profile => profile.IdentityUserId == userId, cancellationToken);
    }

    private bool ApplyValidatedProfile(ProfileEditViewModel model, UserProfile profile)
    {
        model.PreferredTrainingDays = model.PreferredTrainingDays.Where(value => Weekdays.Contains(value, StringComparer.Ordinal)).Distinct(StringComparer.Ordinal).ToList();
        model.Equipment = model.Equipment.Where(value => EquipmentOptions.Contains(value, StringComparer.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (!Enum.IsDefined(model.ExperienceLevel)) ModelState.AddModelError(nameof(model.ExperienceLevel), "Select a valid experience level.");
        if (model.PreferredTrainingDays.Count == 0) ModelState.AddModelError(nameof(model.PreferredTrainingDays), "Choose at least one preferred training day.");
        if (model.Equipment.Count == 0) ModelState.AddModelError(nameof(model.Equipment), "Choose at least one available equipment option.");
        if (model.ProfilePhoto is { Length: > 5_242_880 }) ModelState.AddModelError(nameof(model.ProfilePhoto), "Profile photos must be 5 MB or smaller.");
        if (model.ProfilePhoto is { Length: > 0 } photo)
        {
            var extensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp"
            };
            if (!extensions.TryGetValue(Path.GetExtension(photo.FileName), out var expectedType)
                || !string.Equals(photo.ContentType, expectedType, StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(model.ProfilePhoto), "Choose a JPG, PNG, or WebP image.");
        }
        if (!ModelState.IsValid) return false;
        profile.Name = model.Name.Trim();
        profile.Goal = model.Goal.Trim();
        profile.ExperienceLevel = model.ExperienceLevel;
        profile.PreferredTrainingDays = model.PreferredTrainingDays;
        profile.TrainingDaysPerWeek = (byte)model.PreferredTrainingDays.Count;
        profile.SessionDurationMinutes = (byte)model.SessionDurationMinutes;
        profile.Equipment = model.Equipment;
        return true;
    }

    private async Task SavePhotoAsync(IFormFile? photo, UserProfile profile, CancellationToken cancellationToken)
    {
        if (photo is null || photo.Length == 0) return;
        var extension = Path.GetExtension(photo.FileName);
        var folder = Path.Combine(environment.WebRootPath, "uploads", "profiles");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(folder, fileName);
        await using (var stream = System.IO.File.Create(fullPath)) await photo.CopyToAsync(stream, cancellationToken);
        RemoveOldPhoto(profile.ProfilePhotoPath);
        profile.ProfilePhotoPath = $"/uploads/profiles/{fileName}";
    }

    private void RemoveOldPhoto(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        var fileName = Path.GetFileName(relativePath);
        var oldPath = Path.Combine(environment.WebRootPath, "uploads", "profiles", fileName);
        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
    }

    private void PopulateChoices()
    {
        ViewBag.Goals = Goals;
        ViewBag.Weekdays = Weekdays;
        ViewBag.EquipmentGroups = EquipmentGroups;
    }

    private static ProfileEditViewModel ToViewModel(UserProfile profile) => new()
    {
        Name = profile.Name,
        Goal = profile.Goal,
        ExperienceLevel = profile.ExperienceLevel,
        PreferredTrainingDays = profile.PreferredTrainingDays.ToList(),
        SessionDurationMinutes = profile.SessionDurationMinutes,
        Equipment = profile.Equipment.ToList(),
        CurrentPhotoPath = profile.ProfilePhotoPath
    };
}
