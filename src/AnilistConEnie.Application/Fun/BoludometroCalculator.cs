namespace AnilistConEnie.Application.Fun;

public static class BoludometroCalculator
{
    // Semilla estable por usuario y mes: el historial se mantiene fijo dentro del mes y cambia al siguiente.
    public static int Seed(ulong userId, int anio, int mes) => userId.GetHashCode() ^ (anio * 100 + mes);

    // Genera el porcentaje de "boludez" por día del mes hasta hoy (día → valor 0-100) con tendencias
    // que suben y bajan por tramos. El Random se recibe ya sembrado para que sea determinístico.
    public static Dictionary<int, int> GenerarHistorial(Random rnd, int diaHoy, int totalDiasDelMes)
    {
        int cambiosTendencia = rnd.Next(2, 6);
        Dictionary<int, int> puntosPorDia = new();

        List<int> segmentos = [];
        int puntosRestantes = totalDiasDelMes - 1;
        for (int i = 0; i < cambiosTendencia && puntosRestantes > 0; i++)
        {
            int maxPermitido = puntosRestantes - (cambiosTendencia - i - 1) * 3;
            if (maxPermitido < 3) break;
            int seg = rnd.Next(3, maxPermitido + 1);
            segmentos.Add(seg);
            puntosRestantes -= seg;
        }
        if (puntosRestantes > 0) segmentos.Add(puntosRestantes);

        int actual = rnd.Next(0, 101);
        int dia = 1;
        puntosPorDia[dia++] = actual;
        bool subiendo = rnd.Next(2) == 0;

        foreach (int seg in segmentos)
        {
            int objetivo = subiendo ? rnd.Next(actual + 1, 101) : rnd.Next(0, actual);
            for (int i = 1; i <= seg; i++)
            {
                if (dia > diaHoy) break;
                float t = (float)i / seg;
                int valor = (int)(actual + (objetivo - actual) * t + rnd.Next(-3, 4));
                puntosPorDia[dia++] = Math.Clamp(valor, 0, 100);
            }
            if (dia > diaHoy) break;
            actual = puntosPorDia[dia - 1];
            subiendo = !subiendo;
        }

        return puntosPorDia;
    }
}
