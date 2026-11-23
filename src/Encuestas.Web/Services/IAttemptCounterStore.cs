namespace Encuestas.Web.Services;

/// <summary>
/// Contador de ventana fija compartido entre peticiones (y entre réplicas). Lo usan el rate
/// limiting y el bloqueo por cuenta; la interfaz permite un almacén en memoria en las pruebas.
/// </summary>
public interface IAttemptCounterStore
{
    /// <summary>Intentos registrados para la clave dentro de su ventana vigente.</summary>
    Task<int> CountAsync(string key, TimeSpan window);

    /// <summary>Registra un intento; si la ventana anterior caducó, empieza una nueva.</summary>
    Task IncrementAsync(string key, TimeSpan window);

    Task ResetAsync(string key);
}
