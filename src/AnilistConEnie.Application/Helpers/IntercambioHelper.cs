namespace AnilistConEnie.Application.Helpers;

public static class IntercambioHelper
{
    public static Dictionary<string, int> Shuffle(this Dictionary<string, int> dict) =>
        dict.OrderBy(_ => Random.Shared.Next()).ToDictionary(pair => pair.Key, pair => pair.Value);

    /// <summary>
    /// Algoritmo de reparto que minimiza repeticiones de la combinación (dador → receptor) y reparte
    /// las entregas de la forma más pareja posible según lo pedido por cada persona.
    /// Devuelve la lista de entregas (<c>"dador -&gt; receptor"</c>) y el detalle de cuántas dio cada uno.
    /// </summary>
    public static (List<string> Reparto, Dictionary<string, int> Detalle) RepartirGPT(Dictionary<string, int> pedidos)
    {
        List<string> entregas = [];
        Dictionary<string, int> entregasPorPersona = pedidos.ToDictionary(kvp => kvp.Key, _ => 0);
        Dictionary<(string From, string To), int> combinaciones = [];

        List<string> recepcionesPendientes = pedidos
            .SelectMany(kvp => Enumerable.Repeat(kvp.Key, kvp.Value))
            .ToList();

        foreach (string receptor in recepcionesPendientes)
        {
            string dador = entregasPorPersona
                .Where(kvp => kvp.Key != receptor)
                .Select(kvp =>
                {
                    combinaciones.TryGetValue((kvp.Key, receptor), out int repeticiones);
                    return new
                    {
                        Nombre = kvp.Key,
                        Proporcion = (double)kvp.Value / pedidos[kvp.Key],
                        Entregadas = kvp.Value,
                        Repeticiones = repeticiones
                    };
                })
                .OrderBy(x => x.Repeticiones)
                .ThenBy(x => x.Proporcion)
                .ThenBy(x => x.Entregadas)
                .Select(x => x.Nombre)
                .First();

            entregas.Add($"{dador} -> {receptor}");
            entregasPorPersona[dador]++;
            combinaciones[(dador, receptor)] = combinaciones.GetValueOrDefault((dador, receptor)) + 1;
        }

        return (entregas, entregasPorPersona);
    }

    /// <summary>
    /// Algoritmo clásico de reparto: asigna a cada receptor un dador al azar evitando que se dé a sí
    /// mismo y que se repitan combinaciones, recurriendo a quienes menos dieron cuando no queda opción.
    /// Devuelve la lista ordenada de combinaciones (<c>"dador -&gt; receptor"</c>).
    /// </summary>
    public static List<string> RepartirClasico(Dictionary<string, int> pelisPorRecibir)
    {
        Random rnd = Random.Shared;
        bool exit = false;

        List<string> poolOriginal = [.. pelisPorRecibir.Keys];

        Dictionary<string, Dictionary<string, int>> pelisDadas = [];
        Dictionary<string, int> pelisPorDar = pelisPorRecibir.ToDictionary(p => p.Key, p => p.Value);
        List<string> combinacionesUsadas = [];
        List<string> personasQueDieron = [];

        while (!exit)
        {
            List<string> poolRecibir = [.. pelisPorRecibir.Keys];

            foreach (string personaRecibir in poolRecibir)
            {
                KeyValuePair<string, int> personaDa;
                do
                {
                    personaDa = pelisPorDar.ElementAt(rnd.Next(0, pelisPorDar.Count));

                    if ((pelisPorDar.Count == 1 && pelisPorRecibir.Count == 1) || (pelisPorDar.Count == 1 && pelisPorDar.First().Key == personaRecibir))
                        break;
                }
                while (personaDa.Key == personaRecibir);

                if (pelisPorDar.Count == 1 && pelisPorRecibir.Count == 1)
                {
                    exit = true;
                    break;
                }

                string keyCombinacion = $"{personaDa.Key} -> {personaRecibir}";

                if (combinacionesUsadas.Contains(keyCombinacion)) // Combinacion duplicada, hora de repartir de nuevo
                {
                    List<string> diff = poolOriginal.Except(personasQueDieron).ToList();
                    string newPersonaDa;

                    if (diff.Count > 0) // Si hay alguien que todavia no le dio peli
                    {
                        do
                        {
                            newPersonaDa = diff.ElementAt(rnd.Next(0, diff.Count));
                            keyCombinacion = $"{newPersonaDa} -> {personaRecibir}";
                        } while (newPersonaDa == personaRecibir);
                        combinacionesUsadas.Add(keyCombinacion);
                    }
                    else // Buscar a los que menos hayan dado pelis
                    {
                        do
                        {
                            Dictionary<string, int> pelisPerUser = [];
                            foreach (KeyValuePair<string, Dictionary<string, int>> p in pelisDadas)
                            {
                                foreach (KeyValuePair<string, int> u in p.Value)
                                {
                                    if (!pelisPerUser.TryAdd(u.Key, u.Value))
                                        pelisPerUser[u.Key] += u.Value;
                                }
                            }

                            int min = pelisPerUser.Min(x => x.Value);
                            List<KeyValuePair<string, int>> minMatches = pelisPerUser.Where(x => x.Value == min).ToList();

                            newPersonaDa = minMatches.ElementAt(rnd.Next(0, minMatches.Count)).Key;
                            keyCombinacion = $"{newPersonaDa} -> {personaRecibir}";
                        } while (newPersonaDa == personaRecibir);
                        combinacionesUsadas.Add(keyCombinacion);
                    }

                    // La persona que recibe se le quita una pelicula pedida
                    pelisPorRecibir[personaRecibir] -= 1;
                    if (pelisPorRecibir[personaRecibir] == 0)
                        pelisPorRecibir.Remove(personaRecibir);

                    if (pelisDadas.TryGetValue(newPersonaDa, out Dictionary<string, int>? valuePo))
                    {
                        if (!valuePo.TryAdd(personaRecibir, 1))
                            valuePo[personaRecibir] += 1;
                        pelisDadas[newPersonaDa] = valuePo;
                    }
                    else
                    {
                        pelisDadas.Add(newPersonaDa, new Dictionary<string, int> { { personaRecibir, 1 } });
                    }

                    continue;
                }

                personasQueDieron.Add(personaDa.Key);
                combinacionesUsadas.Add(keyCombinacion);

                if (pelisDadas.TryGetValue(personaDa.Key, out Dictionary<string, int>? value))
                {
                    if (!value.TryAdd(personaRecibir, 1))
                        value[personaRecibir] += 1;
                    pelisDadas[personaDa.Key] = value;
                }
                else
                {
                    pelisDadas.Add(personaDa.Key, new Dictionary<string, int> { { personaRecibir, 1 } });
                }

                // La persona que da se le quita una pelicula por dar
                pelisPorDar[personaDa.Key] -= 1;
                if (pelisPorDar.First(x => x.Key == personaDa.Key).Value == 0)
                    pelisPorDar.Remove(personaDa.Key);

                // La persona que recibe se le quita una pelicula pedida
                pelisPorRecibir[personaRecibir] -= 1;
                if (pelisPorRecibir[personaRecibir] == 0)
                    pelisPorRecibir.Remove(personaRecibir);
            }

            if (pelisPorRecibir.Count == 0)
                exit = true;
        }

        if (pelisPorRecibir.Count > 0)
        {
            Dictionary<string, int> pelisPorRecibirTmp = pelisPorRecibir.ToDictionary(p => p.Key, p => p.Value);

            foreach (KeyValuePair<string, int> personaRecibir in pelisPorRecibirTmp)
            {
                List<string> diff = poolOriginal.Except(personasQueDieron).ToList();
                if (diff.Count != 0)
                {
                    foreach (string dif in diff)
                    {
                        combinacionesUsadas.Add($"{dif} -> {personaRecibir.Key}");

                        pelisPorRecibir[personaRecibir.Key] -= 1;
                        if (pelisPorRecibir[personaRecibir.Key] == 0)
                        {
                            pelisPorRecibir.Remove(personaRecibir.Key);
                            break;
                        }
                    }
                }
            }

            // Si no queda otra que repetir se repite
            foreach (KeyValuePair<string, int> personaRecibir in pelisPorRecibir)
            {
                for (int i = 0; i < personaRecibir.Value; i++)
                {
                    string personaDa;
                    do
                    {
                        personaDa = poolOriginal.ElementAt(rnd.Next(0, poolOriginal.Count));
                    }
                    while (personaDa == personaRecibir.Key);

                    combinacionesUsadas.Add($"{personaDa} -> {personaRecibir.Key}");
                }
            }
        }

        return combinacionesUsadas.OrderBy(x => x).ToList();
    }
}
