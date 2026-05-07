using System;
using System.ServiceModel;
using EnergyConsumptionService.Contracts;
using EnergyConsumptionService.Faults;
using EnergyConsumptionService.Models;
using System.IO;
using EnergyConsumptionService.Events;
using System.Configuration;

namespace EnergyConsumptionService.Services
{
    public class EnergyConsumptionServiceImpl : IEnergyConsumptionService
    {
        public event Action OnTransferStarted;

        public event Action<DailyConsumptionSample> OnSampleReceived;

        public event Action OnTransferCompleted;

        public event EventHandler<WarningEventArgs> OnWarningRaised;

        private string sessionFilePath;

        public void StartSession(SessionMeta meta)
        {
            Console.WriteLine("Prenos u toku...");

            OnTransferStarted?.Invoke();

            Console.WriteLine($"Country: {meta.CountryCode}");
            Console.WriteLine($"Month: {meta.YearMonth}");

            string folderPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data",
                meta.CountryCode,
                meta.YearMonth
            );

            Directory.CreateDirectory(folderPath);

            sessionFilePath = Path.Combine(folderPath, "session.csv");

            if (!File.Exists(sessionFilePath))
            {
                File.WriteAllText(
                    sessionFilePath,
                    "Date,TotalActualMWh,TotalForecastMWh,PeakTime,PeakActualMW,CountryCode"
                    + Environment.NewLine
                );
            }
        }

        public void PushSample(DailyConsumptionSample sample)
        {
            if (sample.TotalActualMWh < 0)
            {
                OnWarningRaised?.Invoke(
                    this,
                    new WarningEventArgs
                    {
                        WarningType = WarningType.ConsumptionOutOfBand,
                        Message = "TotalActualMWh ne sme biti negativan."
                    });

                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "TotalActualMWh ne sme biti negativan.",
                        Field = "TotalActualMWh"
                    });
            }

            if (sample.TotalForecastMWh < 0)
            {
                OnWarningRaised?.Invoke(
                    this,
                    new WarningEventArgs
                    {
                        WarningType = WarningType.ForecastDeviation,
                        Message = "TotalForecastMWh ne sme biti negativan."
                    });

                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "TotalForecastMWh ne sme biti negativan.",
                        Field = "TotalForecastMWh"
                    });
            }

            if (sample.PeakTime.Date != sample.Date.Date)
            {
                OnWarningRaised?.Invoke(
                    this,
                    new WarningEventArgs
                    {
                        WarningType = WarningType.ConsumptionOutOfBand,
                        Message = "PeakTime mora pripadati istom danu."
                    });

                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "PeakTime mora pripadati istom danu.",
                        Field = "PeakTime"
                    });
            }

            double deviationPct =
                Math.Abs(sample.TotalActualMWh - sample.TotalForecastMWh)
                / sample.TotalForecastMWh * 100;

            double threshold =
                double.Parse(ConfigurationManager.AppSettings["ForecastDeviationPct"]);

            if (deviationPct > threshold)
            {
                OnWarningRaised?.Invoke(
                    this,
                    new WarningEventArgs
                    {
                        WarningType = WarningType.ForecastDeviation,
                        Message =
                            $"Veliko odstupanje prognoze za {sample.Date:dd.MM.yyyy} | Odstupanje: {deviationPct:F2}%"
                    });

                Console.WriteLine(
                    $"WARNING: Veliko odstupanje prognoze ({deviationPct:F2}%)");
            }

            Console.WriteLine(
                $"Primljen dnevni agregat za {sample.Date:dd.MM.yyyy}");

            OnSampleReceived?.Invoke(sample);

            string line =
                $"{sample.Date}," +
                $"{sample.TotalActualMWh}," +
                $"{sample.TotalForecastMWh}," +
                $"{sample.PeakTime}," +
                $"{sample.PeakActualMW}," +
                $"{sample.CountryCode}";

            File.AppendAllText(sessionFilePath, line + Environment.NewLine);
        }

        public void EndSession()
        {
            Console.WriteLine("Prenos završen.");

            OnTransferCompleted?.Invoke();
        }
    }
}