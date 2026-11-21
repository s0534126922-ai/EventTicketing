
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventTicketing.Core.DTOs.Auth;
using EventTicketing.Core.Entities;
using EventTicketing.Core.Enums;
using EventTicketing.Core.Exceptions;
using EventTicketing.Core.Interfaces;
using EventTicketing.Core.Interfaces.Repositories;
using EventTicketing.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ValidationException = EventTicketing.Core.Exceptions.ValidationException;

namespace EventTicketing.Service.Services;

public class AuthService : IAuthService
{
    private const int TokenLifetimeMinutes = 60;

    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ValidationException(
                "Registration request is required.");

        var name = request.Name?.Trim() ?? string.Empty;
        var email = NormalizeEmail(request.Email);

        if (string.IsNullOrWhiteSpace(name) ||
            name.Length > 100)
        {
            throw new ValidationException(
                "Name must contain between 1 and 100 characters.");
        }

        ValidateEmail(email);
        ValidatePassword(request.Password);

        if (await _userRepository.EmailExistsAsync(
            email, cancellationToken))
        {
            throw new ConflictException(
                "An account with this email already exists.");
        }

        var user = new User
        {
            FullName = name,
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(
                request.Password!),
            Role = UserRole.User
        };

        await _userRepository.AddAsync(
            user, cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ValidationException(
                "Login request is required.");

        var email = NormalizeEmail(request.Email);

        ValidateEmail(email);

        if (string.IsNullOrEmpty(request.Password))
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        var user = await _userRepository.GetByEmailAsync(
            email, cancellationToken);

        if (user is null ||
            !_passwordHasher.VerifyPassword(
                request.Password,
                user.PasswordHash))
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        return CreateAuthResponse(user);
    }

    private AuthResponseDto CreateAuthResponse(User user)
    {
        var key = _configuration["Jwt:Key"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(key) ||
            Encoding.UTF8.GetByteCount(key) < 32 ||
            string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                "JWT configuration is missing or invalid.");
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(
            TokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),
            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),
            new(
                ClaimTypes.Name,
                user.FullName),
            new(
                ClaimTypes.Email,
                user.Email),
            new(
                ClaimTypes.Role,
                user.Role.ToString()),
            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new AuthResponseDto
        {
            Token = tokenString,
            ExpiresAt = expiresAt,
            User = new UserResponseDto
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString()
            }
        };
    }

    private static string NormalizeEmail(string? email)
    {
        return (email ?? string.Empty)
            .Trim()
            .ToLowerInvariant();
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            email.Length > 256 ||
            !new EmailAddressAttribute().IsValid(email))
        {
            throw new ValidationException(
                "A valid email address is required.");
        }
    }

    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password) ||
            password.Length < 8)
        {
            throw new ValidationException(
                "Password must contain at least 8 characters.");
        }
    }
}
