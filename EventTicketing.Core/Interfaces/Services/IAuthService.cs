using System;
using System.Collections.Generic;
using System.Text;

using EventTicketing.Core.DTOs.Auth;

namespace EventTicketing.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AuthResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default);
}