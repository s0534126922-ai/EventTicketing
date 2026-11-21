
using AutoMapper;
using EventTicketing.Core.DTOs.Halls;
using EventTicketing.Core.Entities;
using EventTicketing.Core.Exceptions;
using EventTicketing.Core.Interfaces;
using EventTicketing.Core.Interfaces.Repositories;
using EventTicketing.Core.Interfaces.Services;

namespace EventTicketing.Service.Services;

public class HallService : IHallService
{
    private readonly IHallRepository _hallRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public HallService(
        IHallRepository hallRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _hallRepository = hallRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<HallResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var halls = await _hallRepository.GetAllAsync(
            cancellationToken);

        return _mapper.Map<List<HallResponseDto>>(halls);
    }

    public async Task<HallResponseDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var hall = await _hallRepository.GetByIdAsync(
            id, cancellationToken);

        if (hall is null)
        {
            throw new NotFoundException(
                "Hall not found.");
        }

        return _mapper.Map<HallResponseDto>(hall);
    }

    public async Task<HallResponseDto> CreateAsync(
        CreateHallRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ValidationException(
                "Hall request is required.");
        }

        var name = ValidateName(request.Name);

        if (request.SeatCount <= 0)
        {
            throw new ValidationException(
                "Seat count must be greater than zero.");
        }

        var hall = new Hall
        {
            Name = name,
            SeatCount = request.SeatCount,
            HasEverHostedEvent = false
        };

        for (int number = 1; number <= request.SeatCount; number++)
        {
            hall.Seats.Add(new Seat
            {
                SeatNumber = number
            });
        }

        await _hallRepository.AddAsync(
            hall, cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return _mapper.Map<HallResponseDto>(hall);
    }

    public async Task UpdateAsync(
        int id,
        UpdateHallRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ValidationException(
                "Hall request is required.");
        }

        var name = ValidateName(request.Name);

        var hall = await _hallRepository.GetByIdAsync(
            id, cancellationToken);

        if (hall is null)
        {
            throw new NotFoundException(
                "Hall not found.");
        }

        hall.Name = name;

        _hallRepository.Update(hall);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    public async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var hall = await _hallRepository.GetByIdWithSeatsAsync(
            id, cancellationToken);

        if (hall is null)
        {
            throw new NotFoundException(
                "Hall not found.");
        }

        var hasHostedEvent =
            hall.HasEverHostedEvent ||
            await _hallRepository.HasEverHostedEventAsync(
                id, cancellationToken);

        if (hasHostedEvent)
        {
            throw new ConflictException(
                "A hall that has hosted an event cannot be deleted.");
        }

        _hallRepository.Delete(hall);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    private static string ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException(
                "Hall name is required.");
        }

        return name.Trim();
    }
}
