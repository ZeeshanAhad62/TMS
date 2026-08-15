using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

public class LoginRequestDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class UserCreateDto
{
    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.Viewer;
}

public class CompanyProfileDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Address { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public class CompanyProfileUpsertDto
{
    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    [EmailAddress, MaxLength(200)]
    public string? ContactEmail { get; set; }

    [MaxLength(50)]
    public string? ContactPhone { get; set; }
}
