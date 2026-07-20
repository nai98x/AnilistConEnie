namespace AnilistConEnie.Application.Fun;

public static class HoroscopoCalculator
{
    // Puntajes 0-100 de amor/salud/dinero: 70% depende del signo y el día (igual para todos los del
    // signo) y 30% de la persona. Determinístico: mismo signo/día/usuario → mismos puntajes.
    public static (double Amor, double Salud, double Dinero) Puntajes(int signo, int dayOfYear, int year, ulong memberId)
    {
        Random rndSignoDay = new(signo + dayOfYear + year);
        Random rndMember = new((int)memberId);

        double amor = Math.Round(0.7 * rndSignoDay.Next(0, 101) + 0.3 * rndMember.Next(0, 101), 0);
        double salud = Math.Round(0.7 * rndSignoDay.Next(0, 101) + 0.3 * rndMember.Next(0, 101), 0);
        double dinero = Math.Round(0.7 * rndSignoDay.Next(0, 101) + 0.3 * rndMember.Next(0, 101), 0);

        return (amor, salud, dinero);
    }
}
