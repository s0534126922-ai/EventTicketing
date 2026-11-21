using System;
using System.Collections.Generic;
using System.Text;

namespace EventTicketing.Core.DTOs.Auth;

public class UserResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}