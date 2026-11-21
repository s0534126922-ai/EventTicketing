using System;
using System.Collections.Generic;
using System.Text;

using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Core.DTOs.Halls;

public class UpdateHallRequestDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
}