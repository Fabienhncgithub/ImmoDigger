using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Application.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace ImmoDigger.Api.Controllers;

[ApiController]
[Route("api/sources")]
public class SourcesController(
    IListingSourceRepository sourceRepository,
    IPropertyListingRepository listingRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SourceDto>>> GetAll(CancellationToken cancellationToken)
    {
        var sources = await sourceRepository.GetAllAsync(cancellationToken);
        var dtos = new List<SourceDto>();

        foreach (var source in sources)
        {
            var count = await listingRepository.CountBySourceAsync(source.Name, cancellationToken);
            dtos.Add(source.ToDto(count));
        }

        return Ok(dtos);
    }

    /// <summary>Toggles activation and/or updates the polling cadence.</summary>
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<SourceDto>> Update(
        Guid id, [FromBody] UpdateSourceRequest request, CancellationToken cancellationToken)
    {
        var source = await sourceRepository.GetByIdAsync(id, cancellationToken);
        if (source is null)
        {
            return NotFound();
        }

        source.IsEnabled = request.IsEnabled;
        if (request.PollingIntervalMinutes is > 0)
        {
            source.PollingIntervalMinutes = request.PollingIntervalMinutes.Value;
        }

        sourceRepository.Update(source);
        await sourceRepository.SaveChangesAsync(cancellationToken);

        var count = await listingRepository.CountBySourceAsync(source.Name, cancellationToken);
        return Ok(source.ToDto(count));
    }
}
