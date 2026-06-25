using AnilistConEnie.Application.Moderation;
using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Tests.Moderation;

public class HackedAccountDetectorTests
{
    private static BasicMessage Msg(string content, ulong channel, DateTime at) => new(content, channel, at);

    private static readonly DateTime Base = new(2026, 6, 25, 12, 0, 0);

    [Fact]
    public void IsHacked_sin_mensajes_es_falso()
    {
        Assert.False(HackedAccountDetector.IsHacked([], canalesDistintos: 3, ventanaMinutos: 5));
    }

    [Fact]
    public void IsHacked_mismo_contenido_en_n_canales_dentro_de_la_ventana()
    {
        List<BasicMessage> mensajes =
        [
            Msg("free nitro", 1, Base),
            Msg("free nitro", 2, Base.AddMinutes(1)),
            Msg("free nitro", 3, Base.AddMinutes(2)),
        ];

        Assert.True(HackedAccountDetector.IsHacked(mensajes, canalesDistintos: 3, ventanaMinutos: 5));
    }

    [Fact]
    public void IsHacked_contenido_distinto_no_es_hackeo()
    {
        List<BasicMessage> mensajes =
        [
            Msg("hola", 1, Base),
            Msg("free nitro", 2, Base.AddMinutes(1)),
            Msg("free nitro", 3, Base.AddMinutes(2)),
        ];

        Assert.False(HackedAccountDetector.IsHacked(mensajes, canalesDistintos: 3, ventanaMinutos: 5));
    }

    [Fact]
    public void IsHacked_pocos_canales_distintos_no_es_hackeo()
    {
        // Mismo contenido pero en 2 canales, no los 3 exigidos.
        List<BasicMessage> mensajes =
        [
            Msg("free nitro", 1, Base),
            Msg("free nitro", 1, Base.AddMinutes(1)),
            Msg("free nitro", 2, Base.AddMinutes(2)),
        ];

        Assert.False(HackedAccountDetector.IsHacked(mensajes, canalesDistintos: 3, ventanaMinutos: 5));
    }

    [Fact]
    public void IsHacked_fuera_de_la_ventana_temporal_no_es_hackeo()
    {
        List<BasicMessage> mensajes =
        [
            Msg("free nitro", 1, Base),
            Msg("free nitro", 2, Base.AddMinutes(1)),
            Msg("free nitro", 3, Base.AddMinutes(10)), // supera la ventana de 5 min
        ];

        Assert.False(HackedAccountDetector.IsHacked(mensajes, canalesDistintos: 3, ventanaMinutos: 5));
    }
}
