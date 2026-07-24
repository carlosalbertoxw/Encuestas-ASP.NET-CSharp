using System.Security.Claims;
using Encuestas.Data;
using Encuestas.Model;
using Encuestas.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encuestas.Tests;

public class AuthServiceTests
{
    private const string Email = "usuario@test.com";
    private const string Password = "Secreta123";

    private readonly PasswordService _passwords = new();
    private readonly AccountLockout _lockout = new(new MemoryCache(new MemoryCacheOptions()));
    private readonly FakeUserRepository _users = new();
    private readonly FakeAuthenticationService _auth = new();

    private AuthService CreateService() =>
        new(_users, _passwords, _lockout, NullLogger<AuthService>.Instance);

    private DefaultHttpContext CreateHttpContext() => new()
    {
        RequestServices = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(_auth)
            .BuildServiceProvider()
    };

    private UserProfile AddUser(bool emailConfirmed = true, string? passwordHash = null)
    {
        var profile = new UserProfile
        {
            User = new User
            {
                Id = 7,
                Email = Email,
                EmailConfirmed = emailConfirmed,
                PasswordHash = passwordHash ?? _passwords.Hash(Password),
                SecurityStamp = "sello-123"
            },
            UserName = "usuario7",
            Name = "Usuario Siete"
        };
        _users.Profile = profile;
        return profile;
    }

    [Fact]
    public async Task Cuenta_bloqueada_devuelve_LockedOut_sin_consultar_el_repositorio()
    {
        AddUser();
        for (var i = 0; i < 5; i++)
        {
            _lockout.RecordFailure(Email);
        }

        var result = await CreateService().LoginAsync(CreateHttpContext(), Email, Password);

        Assert.Equal(LoginResult.LockedOut, result);
        Assert.Equal(0, _users.GetProfileByEmailCalls);
        Assert.Null(_auth.SignedInPrincipal);
    }

    [Fact]
    public async Task Correo_inexistente_devuelve_InvalidCredentials_y_registra_el_fallo()
    {
        var result = await CreateService().LoginAsync(CreateHttpContext(), "nadie@test.com", Password);

        Assert.Equal(LoginResult.InvalidCredentials, result);
        // El intento fallido cuenta para el bloqueo: con 4 fallos más se alcanza el umbral de 5.
        for (var i = 0; i < 4; i++)
        {
            _lockout.RecordFailure("nadie@test.com");
        }
        Assert.True(_lockout.IsLocked("nadie@test.com"));
    }

    [Fact]
    public async Task Password_incorrecta_devuelve_InvalidCredentials_y_no_emite_cookie()
    {
        AddUser();

        var result = await CreateService().LoginAsync(CreateHttpContext(), Email, "incorrecta");

        Assert.Equal(LoginResult.InvalidCredentials, result);
        Assert.Null(_auth.SignedInPrincipal);
    }

    [Fact]
    public async Task Correo_sin_confirmar_devuelve_EmailNotConfirmed_y_no_emite_cookie()
    {
        AddUser(emailConfirmed: false);

        var result = await CreateService().LoginAsync(CreateHttpContext(), Email, Password);

        Assert.Equal(LoginResult.EmailNotConfirmed, result);
        Assert.Null(_auth.SignedInPrincipal);
    }

    [Fact]
    public async Task Login_exitoso_emite_cookie_con_claims_y_limpia_el_contador_de_bloqueo()
    {
        var profile = AddUser();
        for (var i = 0; i < 4; i++)
        {
            _lockout.RecordFailure(Email);
        }

        var result = await CreateService().LoginAsync(CreateHttpContext(), Email, Password);

        Assert.Equal(LoginResult.Success, result);

        var principal = Assert.IsType<ClaimsPrincipal>(_auth.SignedInPrincipal);
        Assert.Equal("7", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(profile.UserName, principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal(profile.User.Email, principal.FindFirstValue(ClaimTypes.Email));
        Assert.Equal(profile.User.SecurityStamp, principal.FindFirstValue(AuthService.SecurityStampClaimType));

        // El contador quedó en cero: 4 fallos nuevos no alcanzan el umbral de 5.
        for (var i = 0; i < 4; i++)
        {
            _lockout.RecordFailure(Email);
        }
        Assert.False(_lockout.IsLocked(Email));
    }

    [Fact]
    public async Task Hash_legado_se_regenera_de_forma_transparente_al_iniciar_sesion()
    {
        var legacyHasher = new PasswordHasher<object>(Options.Create(
            new PasswordHasherOptions { CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2 }));
        var legacyHash = legacyHasher.HashPassword(new object(), Password);
        AddUser(passwordHash: legacyHash);

        var result = await CreateService().LoginAsync(CreateHttpContext(), Email, Password);

        Assert.Equal(LoginResult.Success, result);
        Assert.NotNull(_users.UpdatedPasswordHash);
        Assert.NotEqual(legacyHash, _users.UpdatedPasswordHash);
        Assert.Equal(PasswordVerificationOutcome.Success, _passwords.Verify(_users.UpdatedPasswordHash!, Password));
    }

    /// <summary>Repositorio en memoria con un único perfil y registro de llamadas.</summary>
    private sealed class FakeUserRepository : IUserRepository
    {
        public UserProfile? Profile { get; set; }
        public int GetProfileByEmailCalls { get; private set; }
        public string? UpdatedPasswordHash { get; private set; }

        public Task<UserProfile?> GetProfileByEmailAsync(string email)
        {
            GetProfileByEmailCalls++;
            var match = Profile is not null && string.Equals(Profile.User.Email, email, StringComparison.OrdinalIgnoreCase)
                ? Profile
                : null;
            return Task.FromResult(match);
        }

        public Task<RepositoryResult> UpdatePasswordAsync(int id, string passwordHash)
        {
            UpdatedPasswordHash = passwordHash;
            return Task.FromResult(RepositoryResult.Success);
        }

        public Task<User?> GetUserAsync(int id) =>
            Task.FromResult(Profile?.User.Id == id ? Profile.User : null);

        public Task<UserProfile?> GetProfileByUserNameAsync(string userName) =>
            Task.FromResult<UserProfile?>(null);

        public Task<RepositoryResult> CreateUserAsync(string email, string passwordHash, string securityStamp) =>
            Task.FromResult(RepositoryResult.Success);

        public Task<string?> GetSecurityStampAsync(int id) =>
            Task.FromResult(Profile?.User.SecurityStamp);

        public Task<RepositoryResult> ConfirmEmailAsync(int id) => Task.FromResult(RepositoryResult.Success);
        public Task<RepositoryResult> UpdateNameAsync(int id, string name) => Task.FromResult(RepositoryResult.Success);
        public Task<RepositoryResult> UpdateUserNameAsync(int id, string userName) => Task.FromResult(RepositoryResult.Success);
        public Task<RepositoryResult> UpdateEmailAsync(int id, string email) => Task.FromResult(RepositoryResult.Success);
        public Task<RepositoryResult> ChangePasswordAsync(int id, string passwordHash, string newSecurityStamp) => Task.FromResult(RepositoryResult.Success);
        public Task<RepositoryResult> DeleteAccountAsync(int id) => Task.FromResult(RepositoryResult.Success);
    }

    /// <summary>Captura el principal emitido en lugar de escribir una cookie real.</summary>
    private sealed class FakeAuthenticationService : IAuthenticationService
    {
        public ClaimsPrincipal? SignedInPrincipal { get; private set; }

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            SignedInPrincipal = principal;
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;
    }
}
