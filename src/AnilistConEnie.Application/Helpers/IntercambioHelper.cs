namespace AnilistConEnie.Application.Helpers;

public sealed record PedidosIntercambio(Dictionary<string, int> Pedidos, IReadOnlyList<string> Errores)
{
    public bool EsValido => Errores.Count == 0;
}

public static class IntercambioHelper
{
    /// <summary>
    /// Interpreta el texto del modal (<c>"Eli 1, Nai 1, Pepe 3"</c>, separado por comas o saltos de
    /// línea): el último término de cada entrada es la cantidad pedida y el resto es el nombre.
    /// </summary>
    public static PedidosIntercambio ParsearPedidos(string texto)
    {
        Dictionary<string, int> pedidos = new(StringComparer.OrdinalIgnoreCase);
        List<string> errores = [];

        IEnumerable<string> entradas = texto
            .Split([',', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string entrada in entradas)
        {
            string[] partes = entrada.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (partes.Length < 2 || !int.TryParse(partes[^1], out int cantidad))
            {
                errores.Add($"`{entrada}`: falta la cantidad (formato: `Nombre cantidad`).");
                continue;
            }

            if (cantidad < 1)
            {
                errores.Add($"`{entrada}`: la cantidad debe ser al menos 1.");
                continue;
            }

            string nombre = string.Join(' ', partes[..^1]);
            if (!pedidos.TryAdd(nombre, cantidad))
                errores.Add($"`{nombre}` aparece más de una vez.");
        }

        if (errores.Count == 0 && pedidos.Count < 2)
            errores.Add("Se necesitan al menos 2 personas para repartir.");

        return new PedidosIntercambio(pedidos, errores);
    }

    public static Dictionary<string, int> Shuffle(this Dictionary<string, int> dict) =>
        dict.OrderBy(_ => Random.Shared.Next()).ToDictionary(pair => pair.Key, pair => pair.Value);

    /// <summary>
    /// Algoritmo de reparto que minimiza repeticiones de la combinación (dador → receptor) y reparte
    /// las entregas de la forma más pareja posible según lo pedido por cada persona.
    /// Devuelve la lista de entregas (<c>"dador -&gt; receptor"</c>) y el detalle de cuántas dio cada uno.
    /// </summary>
    public static (List<string> Reparto, Dictionary<string, int> Detalle) Repartir(Dictionary<string, int> pedidos)
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
}
