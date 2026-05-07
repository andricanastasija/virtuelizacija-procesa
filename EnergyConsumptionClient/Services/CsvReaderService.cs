using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnergyConsumptionClient.Models;

namespace EnergyConsumptionClient.Services
{
    internal class CsvReaderService
    {
        public List<DailyConsumptionSample> ReadDailySamples(string filePath, string countryCode, string yearMonth)
        {
            List<DailyConsumptionSample> samples = new List<DailyConsumptionSample>();

            string rejectedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rejected_client.csv");

            using (StreamWriter rejectedWriter = new StreamWriter(rejectedPath, false))
            using (StreamReader reader = new StreamReader(filePath))
            {
                rejectedWriter.WriteLine("RowIndex,Reason,OriginalRow");

                string headerLine = reader.ReadLine();
                string[] headers = headerLine.Split(',');

                string actualColumnName = countryCode + "_load_actual_entsoe_transparency";
                string forecastColumnName = countryCode + "_load_forecast_entsoe_transparency";

                int utcIndex = Array.IndexOf(headers, "utc_timestamp");
                int actualIndex = Array.IndexOf(headers, actualColumnName);
                int forecastIndex = Array.IndexOf(headers, forecastColumnName);

                if (actualIndex == -1)
                {
                    throw new Exception("Ne postoji kolona: " + actualColumnName);
                }

                if (forecastIndex == -1)
                {
                    throw new Exception("Ne postoji kolona: " + forecastColumnName);
                }

                int rowIndex = 1;

                var rowsByDate = new Dictionary<DateTime, List<Tuple<DateTime, double, double, int>>>();

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    rowIndex++;

                    string[] parts = line.Split(',');

                    if (parts.Length <= Math.Max(actualIndex, forecastIndex))
                    {
                        WriteRejected(rejectedWriter, rowIndex, "Nedovoljan broj kolona", line);
                        continue;
                    }

                    DateTime timestamp;
                    double actual;
                    double forecast;

                    if (!DateTime.TryParse(parts[utcIndex], out timestamp))
                    {
                        WriteRejected(rejectedWriter, rowIndex, "Neispravan datum", line);
                        continue;
                    }

                    if (!timestamp.ToString("yyyy-MM").Equals(yearMonth))
                        continue;

                    if (string.IsNullOrWhiteSpace(parts[actualIndex]) || parts[actualIndex] == "NaN")
                    {
                        WriteRejected(rejectedWriter, rowIndex, "Actual vrednost je prazna ili NaN", line);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(parts[forecastIndex]) || parts[forecastIndex] == "NaN")
                    {
                        WriteRejected(rejectedWriter, rowIndex, "Forecast vrednost je prazna ili NaN", line);
                        continue;
                    }

                    if (!double.TryParse(parts[actualIndex], out actual))
                    {
                        WriteRejected(rejectedWriter, rowIndex, "Actual vrednost nije broj", line);
                        continue;
                    }

                    if (!double.TryParse(parts[forecastIndex], out forecast))
                    {
                        WriteRejected(rejectedWriter, rowIndex, "Forecast vrednost nije broj", line);
                        continue;
                    }

                    DateTime date = timestamp.Date;

                    if (!rowsByDate.ContainsKey(date))
                        rowsByDate[date] = new List<Tuple<DateTime, double, double, int>>();

                    rowsByDate[date].Add(Tuple.Create(timestamp, actual, forecast, rowIndex));
                }

                foreach (var day in rowsByDate)
                {
                    double totalActual = day.Value.Sum(x => x.Item2 * 0.5);
                    double totalForecast = day.Value.Sum(x => x.Item3 * 0.5);

                    var peak = day.Value.OrderByDescending(x => x.Item2).First();

                    samples.Add(new DailyConsumptionSample
                    {
                        Date = day.Key,
                        TotalActualMWh = totalActual,
                        TotalForecastMWh = totalForecast,
                        PeakTime = peak.Item1,
                        PeakActualMW = peak.Item2,
                        CountryCode = countryCode,
                        RowIndex = peak.Item4
                    });
                }
            }

            return samples;
        }

        private void WriteRejected(StreamWriter writer, int rowIndex, string reason, string originalRow)
        {
            writer.WriteLine($"{rowIndex},{reason},\"{originalRow}\"");
        }
    }
}