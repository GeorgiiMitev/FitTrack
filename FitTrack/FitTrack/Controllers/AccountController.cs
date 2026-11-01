using FitTrack.Models;
using FitTrack.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _db;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, AppDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register() => View();

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new ApplicationUser { UserName = model.UserName, Email = model.Email };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await EnsureProfileAsync(user);
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Setup", "UserProfiles");
        }
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);
        var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user is not null)
            {
                var profile = await EnsureProfileAsync(user);
                if (!profile.SetupCompleted) return RedirectToAction("Setup", "UserProfiles");
            }
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");
        }
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    private async Task<UserProfile> EnsureProfileAsync(ApplicationUser user)
    {
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(item => item.IdentityUserId == user.Id);
        if (profile is not null) return profile;
        profile = new UserProfile
        {
            IdentityUserId = user.Id,
            Name = user.UserName ?? "FitTrack member",
            Goal = "General fitness",
            ExperienceLevel = FitnessExperienceLevel.Beginner,
            TrainingDaysPerWeek = 3,
            SessionDurationMinutes = 45,
            Equipment = new List<string> { "Bodyweight" }
        };
        _db.UserProfiles.Add(profile);
        await _db.SaveChangesAsync();
        return profile;
    }
}
