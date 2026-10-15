using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Stockroom.Application.Abstractions;
using Stockroom.Application.Common;
using Stockroom.Contracts.Dtos;

namespace Stockroom.Application.Auth;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Password).NotEmpty().MaximumLength(200);
    }
}

public sealed class LoginHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IStockroomDbContext _db;
    private readonly IPasswordHasher _passwords;
    private readonly IJwtTokenGenerator _tokens;

    public LoginHandler(IStockroomDbContext db, IPasswordHasher passwords, IJwtTokenGenerator tokens)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
    }

    public async Task<LoginResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);
        if (!_passwords.Verify(user, command.Password) || user is null)
        {
            throw new UnauthorizedException("Email or password is incorrect.");
        }

        var (token, expiresAtUtc) = _tokens.CreateToken(user);
        return new LoginResponse(token, expiresAtUtc, user.DisplayName, user.Email, user.Role.ToString());
    }
}
