using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Charts;

/// <summary>Plantillas de los gráficos del comando "boludómetro" (Fun).</summary>
public static class FunCharts
{
    public static ChartRequest BoludoGauge(int value) => new()
    {
        Width = 500,
        Height = 300,
        BackgroundColor = "transparent",
        Config = $$"""
        {
            type: 'gauge',
            data: {
                labels: ['Normal', 'Boludo', 'Boludito'],
                datasets: [{ data: [50, 100], value: {{value}}, minValue: 0, backgroundColor: ['green', 'red'], borderWidth: 4 }]
            },
            options: {
                legend: { display: false },
                title: { display: false, text: 'Boludometro' },
                needle: { radiusPercentage: 1, widthPercentage: 1, lengthPercentage: 60, color: '#fff' },
                valueLabel: { fontSize: 35, backgroundColor: 'transparent', color: '#00F0FF', formatter: function (value, context) { return value + '%'; }, bottomMarginPercentage: 10 },
                plugins: { datalabels: { display: 'auto', formatter: function (value, context) { return context.chart.data.labels[context.dataIndex]; }, color: '#fff' } }
            }
        }
        """
    };

    public static ChartRequest BoludoLine(string displayName, IEnumerable<int> dias, IEnumerable<int> valores) => new()
    {
        Width = 500,
        Height = 300,
        BackgroundColor = "transparent",
        Config = $$"""
        {
            type: 'line',
            data: {
                labels: [ {{string.Join(",", dias)}} ],
                datasets: [{ label: 'Boludómetro de {{displayName}}', backgroundColor: 'rgb(255, 99, 132)', borderColor: 'rgb(255, 99, 132)', data: [ {{string.Join(",", valores)}} ], fill: false }]
            },
            "options": { "scales": { "yAxes": [ { "ticks": { "min": 0, "max": 100 } } ] } }
        }
        """
    };
}
