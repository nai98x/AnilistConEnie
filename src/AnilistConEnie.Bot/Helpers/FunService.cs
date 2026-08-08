using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Domain.Enum;
using DSharpPlus;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Helpers;

public enum CategoriaHoroscopo
{
    Bienestar,
    Amor,
    Dinero
}

public class FunService(BotConfiguration config)
{

    public char GetGenero(DiscordMember member, bool uppercase = false)
    {
        if (member.Roles.Any(x => x.Id == config.Roles.GeneroMasculino)) return uppercase ? 'O' : 'o';
        if (member.Roles.Any(x => x.Id == config.Roles.GeneroFemenino)) return uppercase ? 'A' : 'a';

        return '@';
    }

    public string BoluditoLevel(DiscordEmoji loreaEste, DiscordMember member, int numero)
    {
        return numero switch
        {
            0 => "Eres la auténtica anti boludez, tu rechazo hacia lo boludito es impresionante, eres un ejemplo a seguir",
            < 25 => "Chill de cojones, la boludez no es algo que exista en tu persona este día.",
            < 50 => $"Estás bien pero no te descuides porque te puedes volver boludit{GetGenero(member)} de un día para el otro.",
            < 75 => $"Hoy te tocó ser bolud{GetGenero(member)} {loreaEste}",
            < 100 => $"Hoy estas muy bolud{GetGenero(member)}, muchísimo mas de la cuenta, y eso es decir bastante.",
            _ => $"Eres el autentico BOLUDIT{GetGenero(member, true)}, un ejemplo para la sociedad de lo que NO se debe ser. Por el bien mayor, aléjate de la sociedad por este día."
        };
    }

    public static SignoZodiacal GetSignoByBirthday(int dia, int mes)
    {
        return mes switch
        {
            1 => dia <= 19 ? SignoZodiacal.Capricorn : SignoZodiacal.Aquarius,
            2 => dia <= 18 ? SignoZodiacal.Aquarius : SignoZodiacal.Pisces,
            3 => dia <= 20 ? SignoZodiacal.Pisces : SignoZodiacal.Aries,
            4 => dia <= 19 ? SignoZodiacal.Aries : SignoZodiacal.Taurus,
            5 => dia <= 20 ? SignoZodiacal.Taurus : SignoZodiacal.Gemini,
            6 => dia <= 20 ? SignoZodiacal.Gemini : SignoZodiacal.Cancer,
            7 => dia <= 22 ? SignoZodiacal.Cancer : SignoZodiacal.Leo,
            8 => dia <= 22 ? SignoZodiacal.Leo : SignoZodiacal.Virgo,
            9 => dia <= 22 ? SignoZodiacal.Virgo : SignoZodiacal.Libra,
            10 => dia <= 22 ? SignoZodiacal.Libra : SignoZodiacal.Scorpio,
            11 => dia <= 21 ? SignoZodiacal.Scorpio : SignoZodiacal.Sagittarius,
            _ => dia <= 21 ? SignoZodiacal.Sagittarius : SignoZodiacal.Capricorn
        };
    }

    public static DiscordEmoji EmoteOfSignoZodiacal(SignoZodiacal signo)
    {
        return signo switch
        {
            SignoZodiacal.Aquarius => DiscordEmoji.FromUnicode("♒"),
            SignoZodiacal.Pisces => DiscordEmoji.FromUnicode("♓"),
            SignoZodiacal.Aries => DiscordEmoji.FromUnicode("♈"),
            SignoZodiacal.Taurus => DiscordEmoji.FromUnicode("♉"),
            SignoZodiacal.Gemini => DiscordEmoji.FromUnicode("♊"),
            SignoZodiacal.Cancer => DiscordEmoji.FromUnicode("♋"),
            SignoZodiacal.Leo => DiscordEmoji.FromUnicode("♌"),
            SignoZodiacal.Virgo => DiscordEmoji.FromUnicode("♍"),
            SignoZodiacal.Libra => DiscordEmoji.FromUnicode("♎"),
            SignoZodiacal.Scorpio => DiscordEmoji.FromUnicode("♏"),
            SignoZodiacal.Sagittarius => DiscordEmoji.FromUnicode("♐"),
            _ => DiscordEmoji.FromUnicode("♑")
        };
    }

    public static (string Texto, DiscordEmoji Emote) GetHoroscopoCategoria(CategoriaHoroscopo categoria, double valor, Random rnd)
    {
        return categoria switch
        {
            CategoriaHoroscopo.Bienestar => valor switch
            {
                < 25 => (Pick(rnd,
                    "Por favor, sal a tocar pasto, necesitas ver la luz del día cada tanto.",
                    "No puede ser que vivas a chupitos con pelis malas de simulwatch, te va a dar un coma etilico.",
                    "De verdad, deja el lolsito, te está matando más rápido de lo que piensas.",
                    "Tanta obsesión por Nekotina está destruyendo tu sistema inmune.",
                    "¿Qué mamada haces jugando Gachas? Sal a tocar pasto siquiera."), DiscordEmoji.FromUnicode("😖")),
                < 50 => (Pick(rnd,
                    "Es un día normal, tómalo con precaución pero tampoco te rayes.",
                    "No intentes locuras que podría salir muy mál, mientras no lo hagas todo irá bien.",
                    "Hoy evitarás el trabajo duro. Bueno, ya lo has evitado toda tu vida."), DiscordEmoji.FromUnicode("😌")),
                < 75 => (Pick(rnd,
                    "Hoy es un buen día, estás en buenas condiciones.",
                    "Sigue por ese camino, la suerte está de tu lado.",
                    "Chill de cojones."), DiscordEmoji.FromUnicode("😊")),
                _ => (Pick(rnd,
                    "Estás en una condición excelente, ya casi ni pareces un miembro del server.",
                    "Tu salud es ejemplar, estás mamadísimo/a.",
                    "Eres el ejemplo del server en lo que respecta al bienestar personal."), DiscordEmoji.FromUnicode("😁"))
            },
            CategoriaHoroscopo.Amor => valor switch
            {
                < 25 => (Pick(rnd,
                    "Yumiko no es tu waifu y nunca lo será, consíguete una pareja real.",
                    "Deja de buscar waifus en mudae, que solo haces el ridículo.",
                    "Es mas probable que animen la película de Yuri on Ice a que te vaya bien en el amor.",
                    "Simplemente no tienes ninguna posibilidad, debes perder cualquier esperanza.",
                    "Intentas conquistar a una fan de Nana, sal de ahí.",
                    "Tu amor será como una película romántica de Netflix. No te olvides de las palomitas."), DiscordEmoji.FromUnicode("😖")),
                < 50 => (Pick(rnd,
                    "Tu nivel atractivo es el de siempre, no sabría decirte si es bueno o malo.",
                    "Deja de intentarlo, enfócate en otra cosa y te irá mejor.",
                    "Un día normal en lo amoroso, nada fuera de lo común.",
                    "No es día para declararse pero tampoco para rendirte."), DiscordEmoji.FromUnicode("😌")),
                < 75 => (Pick(rnd,
                    "Hoy quizás pasen pueda pasar algo muy bueno, anímate.",
                    "Tu enfoque es el correcto, sigue por ese camino y eventualmente lo lograrás.",
                    "Vas en buen camino, tomas las decisiones correctas y triunfarás."), DiscordEmoji.FromUnicode("😊")),
                _ => (Pick(rnd,
                    "Hoy es tu día, eres la auténtica putería en persona.",
                    "Waifu que buscas en mudae, waifu que consigues.",
                    "Tu habilidad para comunicarte te convertirá en el rey/reina del sexting. ¡Prepárate para emoticones coquetos!"), DiscordEmoji.FromUnicode("😁"))
            },
            _ => valor switch
            {
                < 25 => (Pick(rnd,
                    "Deja de inventarte que trabajas en Nekotina, por favor. Hazte un favor y agarra la pala de verdad.",
                    "Nadeko te está arruinando, tanto el dinero virtual como real. Necesitas ayuda.",
                    "¿Y si probamos con agarrar la pala? Por ahí funciona.",
                    "Tu situación financiera es un desastre de proporciones bíblicas.",
                    "Tu habilidad para ahorrar dinero es tan impresionante que podrías abrir una cuenta bancaria solo para tus centavos."), DiscordEmoji.FromUnicode("😖")),
                < 50 => (Pick(rnd,
                    "No te sobra pero tampoco te falta, de chill bro.",
                    "No te gastes todo tu dinero en gachapon o terminarás en la ruina."), DiscordEmoji.FromUnicode("😌")),
                < 75 => (Pick(rnd,
                    "Con su debido esfuerzo, hoy ganarás mucho dinero.",
                    "Ya estas cerca de subir en el ranking de xp, sigue así."), DiscordEmoji.FromUnicode("😊")),
                _ => (Pick(rnd,
                    "Sigue con esa idea millonaria, ese camino te llevará al éxito.",
                    "Hoy subirás de rango en el servidor, termina ese challenge o sigue cotorreando.",
                    "Tu cuenta bancaria sube al ritmo de la inflación en Argentina."), DiscordEmoji.FromUnicode("😁"))
            }
        };
    }

    private static string Pick(Random rnd, params string[] opciones) => opciones[rnd.Next(0, opciones.Length)];
}
