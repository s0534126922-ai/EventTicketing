using System;
using System.Collections.Generic;
using System.Text;

namespace EventTicketing.Core.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public UserResponseDto User { get; set; } = new();
}
