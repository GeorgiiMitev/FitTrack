using System.Security.Claims;
using FitTrack.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Controllers;

public sealed class CoachController(IAITrainingService coach) : Controller
{
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask([FromForm] string question, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(question) || question.Length > 2000)
            return BadRequest(new { success = false, error = "Enter a question up to 2,000 characters." });
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { success = false, error = "Please sign in again." });
        var result = await coach.GetAdviceAsync(userId, question, cancellationToken);
        return Json(new { success = result.Success, answer = result.Text, error = result.Error });
    }
}
