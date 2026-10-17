using AnilistConEnie.Application.Actividad;

namespace AnilistConEnie.Application.Tests.Actividad;

public class RetencionMensajesTests
{
    [Fact]
    public void FechaMinima_OneDay_ReturnsToday() =>
        Assert.Equal(new DateOnly(2026, 10, 10), RetencionMensajes.FechaMinima(new DateOnly(2026, 10, 10), 1));

    [Fact]
    public void FechaMinima_ThirtyDays_IncludesToday() =>
        Assert.Equal(new DateOnly(2026, 9, 11), RetencionMensajes.FechaMinima(new DateOnly(2026, 10, 10), 30));

    [Fact]
    public void FechaMinima_WindowCrossesYear_ReturnsPreviousYearDate() =>
        Assert.Equal(new DateOnly(2025, 12, 3), RetencionMensajes.FechaMinima(new DateOnly(2026, 1, 1), 30));
}
