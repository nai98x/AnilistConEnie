using AnilistConEnie.Application.Moderation;
using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Application.Tests.Moderation;

public class HackedAccountDetectorTests
{
    private static BasicMessage Msg(string content, ulong channel, DateTime at) => new(content, channel, at);

    private static readonly DateTime Base = new(2026, 6, 25, 12, 0, 0);

    [Fact]
    public void IsHacked_NoMessages_ReturnsFalse()
    {
        Assert.False(HackedAccountDetector.IsHacked([], canalesDistintos: 3, ventanaMinutos: 5));
    }

    [Fact]
    public void IsHacked_SameContentInNChannelsWithinWindow_ReturnsTrue()
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
    public void IsHacked_DifferentContent_ReturnsFalse()
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
    public void IsHacked_FewDistinctChannels_ReturnsFalse()
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
    public void IsHacked_OutsideTimeWindow_ReturnsFalse()
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
