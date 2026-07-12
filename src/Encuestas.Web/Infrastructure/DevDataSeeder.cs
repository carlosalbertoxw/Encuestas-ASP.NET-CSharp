using Encuestas.Data;
using Encuestas.Model;
using Encuestas.Web.Services;

namespace Encuestas.Web.Infrastructure;

/// <summary>
/// Inserta datos de demostración cuando la base de datos está vacía. Solo se invoca en el
/// entorno Development; los datos nunca deben llegar a producción. Las contraseñas se hashean
/// con PBKDF2 mediante <see cref="PasswordService"/>, igual que en el registro real.
/// </summary>
public static class DevDataSeeder
{
    private const string DemoEmail = "demo@encuestas.dev";
    private const string DemoPassword = "demo1234";
    private const string AnaEmail = "ana@encuestas.dev";
    private const string AnaPassword = "ana12345";

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var polls = scope.ServiceProvider.GetRequiredService<IPollRepository>();
        var answers = scope.ServiceProvider.GetRequiredService<IAnswerRepository>();
        var passwords = scope.ServiceProvider.GetRequiredService<PasswordService>();

        if (await users.GetProfileByEmailAsync(DemoEmail) is not null)
        {
            return;
        }

        // Cuenta principal con una encuesta de ejemplo.
        await users.CreateUserAsync(DemoEmail, passwords.Hash(DemoPassword), Guid.NewGuid().ToString());
        var demo = (await users.GetProfileByEmailAsync(DemoEmail))!;
        await users.ConfirmEmailAsync(demo.User.Id);
        await users.UpdateUserNameAsync(demo.User.Id, "demo");
        await users.UpdateNameAsync(demo.User.Id, "Usuario Demo");
        await polls.AddPollAsync(new Poll
        {
            Title = "¿Qué te parece la aplicación?",
            Description = "Cuéntanos tu experiencia usando Encuestas.",
            Position = 1,
            UserId = demo.User.Id
        });

        // Segunda cuenta que responde la encuesta de la primera.
        await users.CreateUserAsync(AnaEmail, passwords.Hash(AnaPassword), Guid.NewGuid().ToString());
        var ana = (await users.GetProfileByEmailAsync(AnaEmail))!;
        await users.ConfirmEmailAsync(ana.User.Id);
        await users.UpdateUserNameAsync(ana.User.Id, "ana");
        await users.UpdateNameAsync(ana.User.Id, "Ana");
        var poll = (await polls.GetPollsAsync(demo.User.Id)).First();
        await answers.AddAnswerAsync(new Answer
        {
            Stars = 5,
            Comment = "¡Muy fácil de usar!",
            PollId = poll.Id,
            UserId = ana.User.Id
        });

        logger.LogInformation("Datos de demostración insertados (solo Development).");
    }
}
