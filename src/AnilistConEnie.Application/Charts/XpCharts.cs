using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Charts;

/// <summary>
/// Plantillas de los gráficos (QuickChart / Chart.js) de los comandos de XP. Centraliza el JSON de
/// configuración para que los comandos solo aporten datos ya calculados.
/// </summary>
public static class XpCharts
{
    /// <summary>Serie de un gráfico de líneas: etiqueta, puntos y color.</summary>
    public readonly record struct LineSeries(string Label, IReadOnlyList<long> Data, string Color);

    /// <summary>Porción de un gráfico de torta: etiqueta, valor y color.</summary>
    public readonly record struct PieSlice(string Label, long Value, string Color);

    public static ChartRequest ProgressBar(long total, long max) => new()
    {
        Width = 500,
        Height = 150,
        Config = $@"{{
                      ""type"": ""horizontalBar"",
                      ""data"": {{
                        ""datasets"": [
                          {{
                            ""label"": ""Dataset 1"",
                            ""data"": [ {total} ],
                            ""backgroundColor"": ""rgba(54, 162, 235, 0.6)"",
                            ""fill"": false,
                            ""type"": ""horizontalBar"",
                            ""borderColor"": ""rgba(54, 162, 235, 0.6)"",
                            ""borderWidth"": 3
                          }}
                        ],
                        ""labels"": [ """" ]
                      }},
                      ""options"": {{
                        ""legend"": {{ ""display"": false }},
                        ""scales"": {{
                          ""xAxes"": [
                            {{
                              ""ticks"": {{
                                ""beginAtZero"": true,
                                ""fontSize"": 17,
                                ""fontColor"": ""#ffffff"",
                                ""fontStyle"": ""bold"",
                                ""min"": 0,
                                ""max"": {max},
                                ""stepSize"": {max}
                              }},
                              ""gridLines"": {{ ""color"": ""rgba(255, 255, 255, 1)"", ""zeroLineColor"": ""rgba(255, 255, 255, 1)"" }}
                            }}
                          ],
                          ""yAxes"": []
                        }},
                        ""plugins"": {{
                          ""datalabels"": {{
                            ""display"": true,
                            ""align"": ""center"",
                            ""anchor"": ""center"",
                            ""backgroundColor"": ""#ffffff"",
                            ""borderColor"": ""#ddd"",
                            ""borderRadius"": 6,
                            ""borderWidth"": 1,
                            ""padding"": 5,
                            ""color"": ""#000000"",
                            ""font"": {{ ""size"": 25 }}
                          }}
                        }}
                      }}
                    }}"
    };

    public static ChartRequest Distribution(IReadOnlyList<PieSlice> slices) => new()
    {
        Width = 500,
        Height = 300,
        Config = $@"{{
                      ""type"": ""pie"",
                      ""data"": {{
                        ""datasets"": [
                          {{
                            ""data"": [ {string.Join(",", slices.Select(x => x.Value))} ],
                            ""backgroundColor"": [ {string.Join(",", slices.Select(x => $"\"{x.Color}\""))} ],
                            ""label"": ""Dataset 1"",
                            ""type"": ""pie"",
                            ""borderColor"": [ {string.Join(",", slices.Select(x => $"\"{x.Color}\""))} ],
                            ""borderWidth"": 3
                          }}
                        ],
                        ""labels"": [ {string.Join(",", slices.Select(x => $"\"{x.Label}\""))} ]
                      }},
                      ""options"": {{
                        ""legend"": {{
                          ""display"": true,
                          ""position"": ""top"",
                          ""labels"": {{ ""fontColor"": ""#ffffff"" }}
                        }},
                        ""plugins"": {{ ""datalabels"": {{ ""display"": false }} }}
                      }}
                    }}"
    };

    public static ChartRequest History(IReadOnlyList<(long Xp, DateTime Date)> points, long min, long max) => new()
    {
        Width = 500,
        Height = 300,
        Config = $@"{{
                      ""type"": ""line"",
                      ""data"": {{
                        ""datasets"": [
                          {{
                            ""label"": ""Experiencia"",
                            ""data"": [ {string.Join(",", points.Select(x => x.Xp))} ],
                            ""fill"": true,
                            ""borderColor"": ""rgb(255, 255, 255)"",
                            ""lineTension"": 0.2,
                            ""type"": ""line"",
                            ""backgroundColor"": ""rgb(4, 172, 255)"",
                            ""borderWidth"": 3
                          }}
                        ],
                        ""labels"": [ {string.Join(",", points.Select(x => $"\"{x.Date:dd/MM/yyyy}\""))} ]
                      }},
                      ""options"": {{
                        ""legend"": {{ ""display"": false }},
                        ""scales"": {{
                          ""xAxes"": [ {{ ""ticks"": {{ ""fontColor"": ""#ffffff"" }}, ""gridLines"": {{ ""display"": false }} }} ],
                          ""yAxes"": [
                            {{
                              ""ticks"": {{
                                ""beginAtZero"": true,
                                ""fontColor"": ""#ffffff"",
                                ""min"": {min},
                                ""max"": {max}
                              }},
                              ""gridLines"": {{ ""color"": ""rgba(255, 255, 255, 1)"", ""zeroLineColor"": ""rgba(255, 255, 255, 1)"" }}
                            }}
                          ]
                        }}
                      }}
                    }}"
    };

    public static ChartRequest TopMultiLine(IReadOnlyList<LineSeries> series, IReadOnlyList<DateTime> labelDates, long min, long max)
    {
        string datasets = string.Join(",", series.Select(s => $@"{{
                            ""label"": ""{s.Label}"",
                            ""data"": [ {string.Join(",", s.Data)} ],
                            ""fill"": false,
                            ""borderColor"": ""{s.Color}"",
                            ""lineTension"": 0.2,
                            ""type"": ""line"",
                            ""backgroundColor"": ""rgb(4, 172, 255)"",
                            ""borderWidth"": 3
                            }}"));

        return new ChartRequest
        {
            Width = 1000,
            Height = 600,
            Config = $@"{{
                      ""type"": ""line"",
                      ""data"": {{
                        ""datasets"": [ {datasets} ],
                        ""labels"": [ {string.Join(",", labelDates.Select(d => $"\"{d.Day}/{d.Month}/{d.Year}\""))} ]
                      }},
                      ""options"": {{
                        ""legend"": {{ ""display"": true, ""position"": ""top"", ""labels"": {{ ""fontSize"": 20, ""fontColor"": ""#ffffff"", ""fontStyle"": ""bold"" }} }},
                        ""scales"": {{
                          ""xAxes"": [ {{ ""display"": false }} ],
                          ""yAxes"": [
                            {{
                              ""ticks"": {{
                                ""beginAtZero"": true,
                                ""fontSize"": 20,
                                ""fontColor"": ""#ffffff"",
                                ""fontStyle"": ""bold"",
                                ""min"": {min},
                                ""max"": {max}
                              }},
                              ""gridLines"": {{ ""color"": ""rgba(255, 255, 255, 1)"", ""zeroLineColor"": ""rgba(255, 255, 255, 1)"" }}
                            }}
                          ]
                        }}
                      }}
                    }}"
        };
    }

    public static ChartRequest CountryPie(IReadOnlyList<PieSlice> slices) => new()
    {
        Width = 500,
        Height = 300,
        Config = $@"{{
                      ""type"": ""pie"",
                      ""data"": {{
                        ""datasets"": [
                          {{
                            ""data"": [ {string.Join(",", slices.Select(x => x.Value))} ],
                            ""backgroundColor"": [ {string.Join(",", slices.Select(x => $"\"{x.Color}\""))} ],
                            ""label"": ""Dataset 1"",
                            ""type"": ""pie"",
                            ""borderColor"": [ {string.Join(",", slices.Select(x => $"\"{x.Color}\""))} ],
                            ""borderWidth"": 3
                          }}
                        ],
                        ""labels"": [ {string.Join(",", slices.Select(x => $"\"{x.Label}\""))} ]
                      }},
                      ""options"": {{
                        ""legend"": {{ ""display"": true, ""position"": ""top"", ""labels"": {{ ""fontColor"": ""#ffffff"" }} }},
                        ""plugins"": {{ ""datalabels"": {{ ""display"": false }} }}
                      }}
                    }}"
    };
}
