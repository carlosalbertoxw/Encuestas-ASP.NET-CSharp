namespace Encuestas.Web.Models;

/// <summary>
/// Mensajes de validación en español para los atributos de los ViewModels (los de .NET vienen en
/// inglés). No usan el nombre del campo ({0}) porque la etiqueta ya está junto al mensaje.
/// </summary>
public static class ValidationMessages
{
    public const string Required = "Este campo es obligatorio";
    public const string MaxLength = "Debe tener como máximo {1} caracteres";
    public const string LengthRange = "Debe tener entre {2} y {1} caracteres";
    public const string Email = "Ingresa un correo electrónico válido";
    public const string PasswordsMatch = "Las contraseñas no coinciden";
    public const string Range = "El valor debe estar entre {1} y {2}";
    public const string UserName = "Solo letras, números y guiones";
    public const string Number = "Ingresa un número";
    public const string InvalidValue = "El valor no es válido";
}
