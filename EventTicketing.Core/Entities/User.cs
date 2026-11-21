using System;
using System.Collections.Generic;
using System.Text;

using EventTicketing.Core.Enums;

namespace EventTicketing.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}