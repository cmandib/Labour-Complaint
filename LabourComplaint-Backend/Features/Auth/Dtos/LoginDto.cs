// Features/Auth/Dtos/LoginDto.cs
public record LoginDto
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
}