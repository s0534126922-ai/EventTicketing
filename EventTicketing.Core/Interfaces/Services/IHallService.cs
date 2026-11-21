using System;
using System.Collections.Generic;
using System.Text;

using EventTicketing.Core.DTOs.Halls;

namespace EventTicketing.Core.Interfaces.Services;

public interface IHallService
{
    Task<IReadOnlyList<HallResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<HallResponseDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<HallResponseDto> CreateAsync(
        CreateHallRequestDto request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        int id,
        UpdateHallRequestDto request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}
