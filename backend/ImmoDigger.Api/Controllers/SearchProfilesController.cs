using FluentValidation;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Application.Mapping;
using ImmoDigger.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ImmoDigger.Api.Controllers;

[ApiController]
[Route("api/search-profiles")]
public class SearchProfilesController(
    ISearchProfileRepository repository,
    IValidator<SearchProfileRequest> validator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SearchProfileDto>>> GetAll(CancellationToken cancellationToken)
    {
        var profiles = await repository.GetAllAsync(cancellationToken);
        return Ok(profiles.Select(p => p.ToDto()).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<SearchProfileDto>> Create(
        [FromBody] SearchProfileRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return BadRequest(ModelState);
        }

        var profile = new SearchProfile { Name = request.Name };
        profile.ApplyRequest(request);

        await repository.AddAsync(profile, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { id = profile.Id }, profile.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SearchProfileDto>> Update(
        Guid id, [FromBody] SearchProfileRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return BadRequest(ModelState);
        }

        var profile = await repository.GetByIdAsync(id, cancellationToken);
        if (profile is null)
        {
            return NotFound();
        }

        profile.ApplyRequest(request);
        repository.Update(profile);
        await repository.SaveChangesAsync(cancellationToken);

        return Ok(profile.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var profile = await repository.GetByIdAsync(id, cancellationToken);
        if (profile is null)
        {
            return NotFound();
        }

        repository.Remove(profile);
        await repository.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
