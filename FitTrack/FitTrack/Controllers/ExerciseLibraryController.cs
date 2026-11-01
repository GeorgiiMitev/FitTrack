using FitTrack.Models;
using FitTrack.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Controllers;

public sealed class ExerciseLibraryController(IExerciseApiService exerciseApi) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? muscle, string? category, string? equipment, CancellationToken cancellationToken)
    {
        var catalog = await exerciseApi.GetExercisesAsync(cancellationToken);
        var filtered = catalog.Exercises.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search)) filtered = filtered.Where(item => item.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(muscle)) filtered = filtered.Where(item => item.Muscles.Any(value => value.Equals(muscle, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(category)) filtered = filtered.Where(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(equipment)) filtered = filtered.Where(item => item.Equipment.Any(value => value.Equals(equipment, StringComparison.OrdinalIgnoreCase)));
        var results = filtered.OrderBy(item => item.Name).ToList();
        return View(new ExerciseLibraryViewModel
        {
            Search = search, Muscle = muscle, Category = category, Equipment = equipment,
            Error = catalog.Error, UsingCachedData = catalog.IsUsingCache,
            Muscles = catalog.Exercises.SelectMany(item => item.Muscles).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList(),
            Categories = catalog.Exercises.Select(item => item.Category).Where(value => value.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList(),
            EquipmentOptions = catalog.Exercises.SelectMany(item => item.Equipment).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList(),
            Exercises = results
        });
    }
}
