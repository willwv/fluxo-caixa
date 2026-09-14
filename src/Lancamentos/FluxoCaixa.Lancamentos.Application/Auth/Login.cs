using FluentValidation;
using FluxoCaixa.Lancamentos.Application.Common;
using FluxoCaixa.Lancamentos.Application.Interfaces;
using MediatR;

namespace FluxoCaixa.Lancamentos.Application.Auth;

public record LoginCommand(string Username, string Password) : IRequest<LoginResultDto?>;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginHandler : IRequestHandler<LoginCommand, LoginResultDto?>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public LoginHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResultDto?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.ObterPorUsernameAsync(request.Username, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return null;

        var token = _tokenGenerator.GerarToken(user.Id, user.Username);
        return new LoginResultDto(token, DateTime.UtcNow.AddHours(1));
    }
}
