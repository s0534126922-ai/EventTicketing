namespace EventTicketing.Core.DTOs.Bookings;

public class AdminBookingResponseDto
{
    public BookingResponseDto Booking { get; set; } = new();

    public string UserName { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;
}
