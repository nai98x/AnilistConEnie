using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Application.Xp;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;

namespace AnilistConEnie.Application.Tests.Xp;

public class XpChartServiceTests
{
    private sealed class FakeXpDiarioRepo(List<UserDailyXp> chart) : IXpDiarioRepository
    {
        public (ulong UserId, IReadOnlyList<UserDailyXp> Dias)? InsertBulkCall;

        public Task<List<UserDailyXp>> ObtenerChart(ulong userId) => Task.FromResult(chart);

        public Task InsertBulk(ulong userId, IReadOnlyList<UserDailyXp> dias)
        {
            InsertBulkCall = (userId, dias);
            return Task.CompletedTask;
        }

        public Task<List<UserDailyXp>> ObtenerBaseline(DateOnly fecha) => Task.FromResult(new List<UserDailyXp>());
        public Task Upsert(ulong userId, DateTime fecha, long xp) => Task.CompletedTask;
        public Task Snapshot(DateOnly fecha, IReadOnlyList<UserXp> usuarios) => Task.CompletedTask;
    }

    private static UserDailyXp Dia(long userId, DateTime fecha, long xp) =>
        new() { UserId = userId, Date = fecha, Xp = xp };

    [Fact]
    public async Task GetUserChartHistory_DevuelveLosPuntosDelBuilder()
    {
        const ulong userId = 42;
        List<UserDailyXp> historial =
        [
            Dia(42, RelojServidor.Hoy.AddDays(-3), 100),
            Dia(42, RelojServidor.Hoy.AddDays(-1), 300)
        ];
        FakeXpDiarioRepo repo = new(historial);
        XpChartService service = new(repo);

        XpChartResult esperado = XpChartBuilder.Build((long)userId, historial, RelojServidor.Hoy, includeZeroXp: false, fillMissingDays: true);
        List<UserDailyXp> resultado = await service.GetUserChartHistory(userId, includeZeroXp: false, rellenarDiasFaltantes: true);

        Assert.Equal(esperado.Points.Select(p => (p.Date, p.Xp)), resultado.Select(p => (p.Date, p.Xp)));
    }

    [Fact]
    public async Task GetUserChartHistory_PersisteLosDiasFaltantesQueDevuelveElBuilder()
    {
        const ulong userId = 7;
        List<UserDailyXp> historial =
        [
            Dia(7, RelojServidor.Hoy.AddDays(-5), 50),
            Dia(7, RelojServidor.Hoy, 500)
        ];
        FakeXpDiarioRepo repo = new(historial);
        XpChartService service = new(repo);

        XpChartResult esperado = XpChartBuilder.Build((long)userId, historial, RelojServidor.Hoy, includeZeroXp: false, fillMissingDays: true);
        await service.GetUserChartHistory(userId, includeZeroXp: false, rellenarDiasFaltantes: true);

        Assert.Equal(esperado.MissingDaysToPersist.Count > 0, repo.InsertBulkCall is not null);
        if (esperado.MissingDaysToPersist.Count > 0)
        {
            Assert.Equal(userId, repo.InsertBulkCall!.Value.UserId);
            Assert.Equal(esperado.MissingDaysToPersist.Count, repo.InsertBulkCall.Value.Dias.Count);
        }
    }

    [Fact]
    public async Task GetUserChartHistory_SinRelleno_NoPersiste()
    {
        FakeXpDiarioRepo repo = new([Dia(1, RelojServidor.Hoy.AddDays(-2), 10)]);
        XpChartService service = new(repo);

        await service.GetUserChartHistory(1, includeZeroXp: false, rellenarDiasFaltantes: false);

        Assert.Null(repo.InsertBulkCall);
    }

    [Fact]
    public async Task GetUserWeeklyHistory_CoincideConXpTopChartHistory()
    {
        const ulong userId = 99;
        const long currentXp = 1234;
        List<UserDailyXp> historial =
        [
            Dia(99, RelojServidor.Hoy.AddDays(-10), 200),
            Dia(99, RelojServidor.Hoy.AddDays(-3), 800)
        ];
        FakeXpDiarioRepo repo = new(historial);
        XpChartService service = new(repo);

        List<UserDailyXp> esperado = XpTopChartHistory.Build((long)userId, historial, RelojServidor.Hoy, currentXp);
        List<UserDailyXp> resultado = await service.GetUserWeeklyHistory(userId, currentXp);

        Assert.Equal(esperado.Select(p => (p.Date, p.Xp)), resultado.Select(p => (p.Date, p.Xp)));
    }
}
