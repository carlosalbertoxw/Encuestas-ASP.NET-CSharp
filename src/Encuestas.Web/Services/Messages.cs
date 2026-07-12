namespace Encuestas.Web.Services;

/// <summary>Mensajes de usuario centralizados para no repetir literales entre controladores.</summary>
public static class Messages
{
    public const string ValidationError = "Ocurrió un error en la validación de los datos";
    public const string WrongPassword = "La contraseña es incorrecta";
    public const string UpdateOk = "Los datos se actualizaron exitosamente";
    public const string UpdateError = "Ocurrió un error al actualizar los datos";

    public const string InvalidCredentials = "Correo o contraseña incorrectos";
    // SEG-03: mismo mensaje tanto si el correo ya existía como si el registro procedió,
    // para no revelar qué correos tienen cuenta.
    public const string RegisterAcknowledged = "Si los datos son válidos, te enviamos un correo para confirmar la cuenta.";
    public const string UserNameTaken = "El nombre de usuario no está disponible";
    public const string EmailNotConfirmed = "Debes confirmar tu correo antes de iniciar sesión. Revisa tu bandeja de entrada.";
    public const string AccountLocked = "Demasiados intentos fallidos. Intenta de nuevo en unos minutos.";
    public const string EmailConfirmed = "Tu correo fue confirmado. Ya puedes iniciar sesión.";
    public const string EmailConfirmationInvalid = "El enlace de confirmación no es válido o expiró.";
    public const string PasswordResetSent = "Si el correo corresponde a una cuenta, te enviamos un enlace para restablecer la contraseña.";
    public const string PasswordResetOk = "Tu contraseña se restableció. Ya puedes iniciar sesión.";
    public const string PasswordResetInvalid = "El enlace para restablecer la contraseña no es válido o expiró.";

    public const string PollSaved = "Los datos se guardaron exitosamente";
    public const string PollUpdated = "Los datos se actualizaron exitosamente";
    public const string PollDeleted = "Los datos se borraron exitosamente";
    public const string PollSaveError = "Ocurrió un error al guardar los datos";
    public const string PollDeleteError = "Ocurrió un error al borrar los datos";

    public const string AnswerSaved = "La respuesta se guardó exitosamente";
    public const string AnswerError = "Ocurrió un error al guardar la respuesta";
    public const string AnswerDuplicate = "Ya habías respondido esta encuesta";

    public const string AccountDeleted = "La cuenta se eliminó exitosamente";
    public const string AccountDeleteError = "Ocurrió un error al eliminar la cuenta";
}
