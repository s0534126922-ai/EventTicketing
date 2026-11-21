using System;
using System.Collections.Generic;
using System.Text;

using EventTicketing.Core.DTOs.Bookings;

namespace EventTicketing.Core.Interfaces.Services;

public interface IBookingService
{
    Task<BookingConfirmationResponseDto> CreateAsync(
        int userId,
        CreateBookingRequestDto request,
        CancellationToken cancellationToken = default);

    Task<UserBookingsResponseDto> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task CancelAsync(
        int bookingId,
        int userId,
        CancellationToken cancellationToken = default);
}