using AnilistConEnie.Application.Triggers;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Enum;

namespace AnilistConEnie.Application.Tests.Triggers;

public class TriggerMatcherTests
{
    [Theory]
    [InlineData(TipoTrigger.TEXTO_EXACTO, "hola", "hola", true)]
    [InlineData(TipoTrigger.TEXTO_EXACTO, "hola que tal", "hola", false)]
    [InlineData(TipoTrigger.TERMINA_EN, "buenas hola", "hola", true)]
    [InlineData(TipoTrigger.TERMINA_EN, "hola buenas", "hola", false)]
    [InlineData(TipoTrigger.EMPIEZA_CON, "hola buenas", "hola", true)]
    [InlineData(TipoTrigger.EMPIEZA_CON, "buenas hola", "hola", false)]
    [InlineData(TipoTrigger.LIBRE, "che hola che", "hola", true)]
    [InlineData(TipoTrigger.LIBRE, "chau", "hola", false)]
    public void Coincide_PerTipo_AppliesMatchingRule(TipoTrigger tipo, string mensaje, string clave, bool esperado)
    {
        Assert.Equal(esperado, TriggerMatcher.Coincide(mensaje, clave, tipo));
    }

    [Fact]
    public void Aplicables_MixedCaseMessage_ReturnsMatching()
    {
        Dictionary<string, Trigger> triggers = new()
        {
            ["hola"] = new Trigger { Nombre = "hola", Tipo = (int)TipoTrigger.LIBRE },
            ["chau"] = new Trigger { Nombre = "chau", Tipo = (int)TipoTrigger.LIBRE },
        };

        List<Trigger> resultado = TriggerMatcher.Aplicables("Buenas HOLA gente", triggers);

        Assert.Single(resultado);
        Assert.Equal("hola", resultado[0].Nombre);
    }

    [Fact]
    public void Aplicables_NoMatch_ReturnsEmpty()
    {
        Dictionary<string, Trigger> triggers = new()
        {
            ["hola"] = new Trigger { Nombre = "hola", Tipo = (int)TipoTrigger.TEXTO_EXACTO },
        };

        Assert.Empty(TriggerMatcher.Aplicables("hola gente", triggers));
    }
}
