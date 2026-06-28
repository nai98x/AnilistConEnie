using AnilistConEnie.Application.Membership;

namespace AnilistConEnie.Application.Tests.Membership;

public class AniversarioCalculatorTests
{
    private const int GuildCreationYear = 2020;

    [Fact]
    public void EsAniversarioAhora_SameDayAndHourOneYearLater_ReturnsTrue()
    {
        DateTime now = new(2026, 6, 25, 14, 0, 0);
        DateTimeOffset entrada = new(2025, 6, 25, 14, 30, 0, TimeSpan.Zero);

        Assert.True(AniversarioCalculator.EsAniversarioAhora(entrada, now, GuildCreationYear));
    }

    [Fact]
    public void EsAniversarioAhora_DifferentHour_ReturnsFalse()
    {
        DateTime now = new(2026, 6, 25, 14, 0, 0);
        DateTimeOffset entrada = new(2025, 6, 25, 9, 0, 0, TimeSpan.Zero);

        Assert.False(AniversarioCalculator.EsAniversarioAhora(entrada, now, GuildCreationYear));
    }

    [Fact]
    public void EsAniversarioAhora_DifferentDay_ReturnsFalse()
    {
        DateTime now = new(2026, 6, 25, 14, 0, 0);
        DateTimeOffset entrada = new(2025, 6, 24, 14, 0, 0, TimeSpan.Zero);

        Assert.False(AniversarioCalculator.EsAniversarioAhora(entrada, now, GuildCreationYear));
    }

    [Fact]
    public void EsAniversarioAhora_SeveralYearsWithinTenure_ReturnsTrue()
    {
        DateTime now = new(2026, 6, 25, 14, 0, 0);
        DateTimeOffset entrada = new(2023, 6, 25, 14, 0, 0, TimeSpan.Zero); // 3 años

        Assert.True(AniversarioCalculator.EsAniversarioAhora(entrada, now, GuildCreationYear));
    }

    [Fact]
    public void EsAniversarioAhora_JoinedToday_ReturnsFalse()
    {
        // Entró hoy mismo: aún no cumplió ningún año.
        DateTime now = new(2026, 6, 25, 14, 0, 0);
        DateTimeOffset entrada = new(2026, 6, 25, 14, 0, 0, TimeSpan.Zero);

        Assert.False(AniversarioCalculator.EsAniversarioAhora(entrada, now, GuildCreationYear));
    }

    [Theory]
    [InlineData(2023, 6, 25, 3)]   // cumplió justo hoy
    [InlineData(2023, 6, 24, 3)]   // ya pasó el día
    [InlineData(2023, 6, 26, 2)]   // todavía no llega el día este año
    [InlineData(2023, 12, 31, 2)]  // falta para fin de año
    public void AniosEnServidor_CountsCompletedYears(int year, int month, int day, int esperado)
    {
        DateTime now = new(2026, 6, 25);
        DateTimeOffset entrada = new(year, month, day, 0, 0, 0, TimeSpan.Zero);

        Assert.Equal(esperado, AniversarioCalculator.AniosEnServidor(entrada, now));
    }
}
