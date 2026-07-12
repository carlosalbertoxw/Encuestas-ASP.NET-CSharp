using Encuestas.Data;
using Encuestas.Model;

namespace Encuestas.Tests;

[Collection("mysql")]
public class RepositoryIntegrationTests
{
    private readonly UserRepository _users;
    private readonly PollRepository _polls;
    private readonly AnswerRepository _answers;

    public RepositoryIntegrationTests(MySqlContainerFixture fixture)
    {
        _users = new UserRepository(fixture.DataSource);
        _polls = new PollRepository(fixture.DataSource);
        _answers = new AnswerRepository(fixture.DataSource);
    }

    // Cada test usa un correo propio para no depender del orden de ejecución.

    [Fact]
    public async Task CreateUser_crea_cuenta_y_perfil_en_una_transaccion()
    {
        Assert.Equal(RepositoryResult.Success, await _users.CreateUserAsync("alta@test.com", "hash", "stamp-1"));

        var profile = await _users.GetProfileByEmailAsync("alta@test.com");
        Assert.NotNull(profile);
        Assert.Equal("usuario" + profile.User.Id, profile.UserName);
        Assert.Equal("hash", profile.User.PasswordHash);
        Assert.Equal("stamp-1", profile.User.SecurityStamp);
    }

    [Fact]
    public async Task CreateUser_rechaza_correo_duplicado()
    {
        Assert.Equal(RepositoryResult.Success, await _users.CreateUserAsync("duplicado@test.com", "hash", "s"));
        Assert.Equal(RepositoryResult.Duplicate, await _users.CreateUserAsync("duplicado@test.com", "hash", "s"));
    }

    [Fact]
    public async Task UpdateUserName_distingue_duplicado_de_no_encontrado()
    {
        await _users.CreateUserAsync("nombre1@test.com", "hash", "s");
        await _users.CreateUserAsync("nombre2@test.com", "hash", "s");
        var first = await _users.GetProfileByEmailAsync("nombre1@test.com");
        var second = await _users.GetProfileByEmailAsync("nombre2@test.com");

        Assert.Equal(RepositoryResult.Success, await _users.UpdateUserNameAsync(first!.User.Id, "nombre-unico"));
        Assert.Equal(RepositoryResult.Duplicate, await _users.UpdateUserNameAsync(second!.User.Id, "nombre-unico"));
        Assert.Equal(RepositoryResult.NotFound, await _users.UpdateUserNameAsync(999999, "otro-nombre"));
    }

    [Fact]
    public async Task ChangePassword_rota_el_sello_de_seguridad()
    {
        await _users.CreateUserAsync("stamp@test.com", "hash", "stamp-original");
        var userId = (await _users.GetProfileByEmailAsync("stamp@test.com"))!.User.Id;

        Assert.Equal(RepositoryResult.Success, await _users.ChangePasswordAsync(userId, "hash2", "stamp-nuevo"));
        Assert.Equal("stamp-nuevo", await _users.GetSecurityStampAsync(userId));
    }

    [Fact]
    public async Task UpdatePassword_conserva_el_sello_de_seguridad()
    {
        await _users.CreateUserAsync("rehash@test.com", "hash", "stamp-fijo");
        var userId = (await _users.GetProfileByEmailAsync("rehash@test.com"))!.User.Id;

        Assert.Equal(RepositoryResult.Success, await _users.UpdatePasswordAsync(userId, "hash-mejorado"));
        Assert.Equal("stamp-fijo", await _users.GetSecurityStampAsync(userId));
    }

    [Fact]
    public async Task Polls_solo_son_visibles_y_editables_por_su_propietario()
    {
        await _users.CreateUserAsync("dueno@test.com", "hash", "s");
        await _users.CreateUserAsync("intruso@test.com", "hash", "s");
        var owner = (await _users.GetProfileByEmailAsync("dueno@test.com"))!.User.Id;
        var intruder = (await _users.GetProfileByEmailAsync("intruso@test.com"))!.User.Id;

        await _polls.AddPollAsync(new Poll { Title = "Mía", Description = "d", Position = 1, UserId = owner });
        var poll = Assert.Single(await _polls.GetPollsAsync(owner));

        Assert.Null(await _polls.GetPollAsync(intruder, poll.Id));
        Assert.False(await _polls.DeletePollAsync(intruder, poll.Id));
        Assert.True(await _polls.DeletePollAsync(owner, poll.Id));
    }

    [Fact]
    public async Task DeleteAccount_borra_en_cascada_perfil_encuestas_y_respuestas()
    {
        await _users.CreateUserAsync("cascada@test.com", "hash", "s");
        var userId = (await _users.GetProfileByEmailAsync("cascada@test.com"))!.User.Id;
        await _polls.AddPollAsync(new Poll { Title = "t", Description = "d", Position = 1, UserId = userId });
        var poll = (await _polls.GetPollsAsync(userId)).Single();
        await _answers.AddAnswerAsync(new Answer { Stars = 5, Comment = "c", PollId = poll.Id, UserId = userId });

        Assert.Equal(RepositoryResult.Success, await _users.DeleteAccountAsync(userId));

        Assert.Null(await _users.GetProfileByEmailAsync("cascada@test.com"));
        Assert.Null(await _polls.GetPollByIdAsync(poll.Id));
        Assert.Empty((await _answers.GetAnswersForPollAsync(poll.Id, 1, 10)).Items);
    }

    [Fact]
    public async Task Answers_se_paginan_y_traen_el_nombre_de_usuario()
    {
        await _users.CreateUserAsync("encuestador@test.com", "hash", "s");
        var ownerId = (await _users.GetProfileByEmailAsync("encuestador@test.com"))!.User.Id;
        await _polls.AddPollAsync(new Poll { Title = "t", Description = "d", Position = 1, UserId = ownerId });
        var poll = (await _polls.GetPollsAsync(ownerId)).Single();

        // 12 usuarios distintos responden una vez cada uno (UNIQUE por usuario y encuesta).
        for (var i = 0; i < 12; i++)
        {
            await _users.CreateUserAsync($"resp{i}@test.com", "hash", "s");
            var responderId = (await _users.GetProfileByEmailAsync($"resp{i}@test.com"))!.User.Id;
            Assert.Equal(RepositoryResult.Success,
                await _answers.AddAnswerAsync(new Answer { Stars = 3, Comment = "c" + i, PollId = poll.Id, UserId = responderId }));
        }

        var firstPage = await _answers.GetAnswersForPollAsync(poll.Id, page: 1, pageSize: 10);
        Assert.Equal(12, firstPage.TotalCount);
        Assert.Equal(10, firstPage.Items.Count);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.True(firstPage.HasNext);
        Assert.False(firstPage.HasPrevious);
        Assert.StartsWith("usuario", firstPage.Items[0].UserName);

        var secondPage = await _answers.GetAnswersForPollAsync(poll.Id, page: 2, pageSize: 10);
        Assert.Equal(2, secondPage.Items.Count);
        Assert.True(secondPage.HasPrevious);
        Assert.False(secondPage.HasNext);
    }

    [Fact]
    public async Task AddAnswer_rechaza_una_segunda_respuesta_del_mismo_usuario()
    {
        await _users.CreateUserAsync("dueno-enc@test.com", "hash", "s");
        await _users.CreateUserAsync("respondedor@test.com", "hash", "s");
        var ownerId = (await _users.GetProfileByEmailAsync("dueno-enc@test.com"))!.User.Id;
        var responderId = (await _users.GetProfileByEmailAsync("respondedor@test.com"))!.User.Id;
        await _polls.AddPollAsync(new Poll { Title = "t", Description = "d", Position = 1, UserId = ownerId });
        var poll = (await _polls.GetPollsAsync(ownerId)).Single();

        Assert.Equal(RepositoryResult.Success,
            await _answers.AddAnswerAsync(new Answer { Stars = 4, Comment = "a", PollId = poll.Id, UserId = responderId }));
        Assert.Equal(RepositoryResult.Duplicate,
            await _answers.AddAnswerAsync(new Answer { Stars = 1, Comment = "b", PollId = poll.Id, UserId = responderId }));
    }

    [Fact]
    public async Task ConfirmEmail_marca_la_cuenta_como_confirmada()
    {
        await _users.CreateUserAsync("confirmar@test.com", "hash", "s");
        var userId = (await _users.GetProfileByEmailAsync("confirmar@test.com"))!.User.Id;

        Assert.False((await _users.GetUserAsync(userId))!.EmailConfirmed);
        Assert.Equal(RepositoryResult.Success, await _users.ConfirmEmailAsync(userId));
        Assert.True((await _users.GetUserAsync(userId))!.EmailConfirmed);
    }
}
