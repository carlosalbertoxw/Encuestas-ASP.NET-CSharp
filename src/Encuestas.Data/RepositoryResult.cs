namespace Encuestas.Data;

/// <summary>
/// Resultado de una operación de escritura. Distingue explícitamente los casos que antes
/// se colapsaban en un <c>bool</c>, para que el controlador muestre el mensaje correcto.
/// </summary>
public enum RepositoryResult
{
    /// <summary>La operación afectó la fila esperada.</summary>
    Success,

    /// <summary>Violó una restricción de unicidad (correo o nombre de usuario ya en uso).</summary>
    Duplicate,

    /// <summary>No existe la fila objetivo (0 filas afectadas).</summary>
    NotFound
}
