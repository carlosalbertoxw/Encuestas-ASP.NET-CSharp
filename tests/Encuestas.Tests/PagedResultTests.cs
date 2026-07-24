using Encuestas.Data;

namespace Encuestas.Tests;

public class PagedResultTests
{
    private static PagedResult<int> Create(int page, int pageSize, long totalCount) =>
        new(Array.Empty<int>(), page, pageSize, totalCount);

    [Fact]
    public void TotalPages_redondea_hacia_arriba()
    {
        Assert.Equal(3, Create(page: 1, pageSize: 10, totalCount: 21).TotalPages);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageSize_no_positivo_devuelve_cero_paginas_sin_dividir_entre_cero(int pageSize)
    {
        Assert.Equal(0, Create(page: 1, pageSize: pageSize, totalCount: 21).TotalPages);
    }

    [Theory]
    [InlineData(1, false, true)]
    [InlineData(2, true, true)]
    [InlineData(3, true, false)]
    public void HasPrevious_y_HasNext_segun_la_pagina(int page, bool hasPrevious, bool hasNext)
    {
        var result = Create(page, pageSize: 10, totalCount: 30);

        Assert.Equal(hasPrevious, result.HasPrevious);
        Assert.Equal(hasNext, result.HasNext);
    }

    [Fact]
    public void Resultado_vacio_no_tiene_paginas_ni_navegacion()
    {
        var result = Create(page: 1, pageSize: 10, totalCount: 0);

        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPrevious);
        Assert.False(result.HasNext);
    }
}
