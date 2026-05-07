using System;
using System.ServiceModel;
using EnergyConsumptionService.Contracts;
using EnergyConsumptionService.Faults;
using EnergyConsumptionService.Models;
using System.IO;
using EnergyConsumptionService.Events;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace EnergyConsumptionService.Services
{
    public class EnergyConsumptionServiceImpl : IEnergyConsumptionService
    {
        public event Action OnTransferStarted;

        public event Action<DailyConsumptionSample> OnSampleReceived;

        public event Action OnTransferCompleted;

        public event EventHandler<WarningEventArgs> OnWarningRaised;

        private string sessionFilePath;

        private string rejectsFilePath;

        private List<double> actualConsumptions = new List<double>();

        private List<DailyConsumptionSample> receivedSamples =
            new List<DailyConsumptionSample>();

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

            rejectsFilePath = Path.Combine(folderPath, "rejects.csv");

            if (!File.Exists(sessionFilePath))
            {
                File.WriteAllText(
                    sessionFilePath,
                    "Date,TotalActualMWh,TotalForecastMWh,PeakTime,PeakActualMW,CountryCode"
                    + Environment.NewLine
                );
            }

            if (!File.Exists(rejectsFilePath))
            {
                File.WriteAllText(
                    rejectsFilePath,
                    "Reason,OriginalRow" + Environment.NewLine
                );
            }
        }

        public void PushSample(DailyConsumptionSample sample)
        {
            if (sample.TotalActualMWh < 0)
            {
                File.AppendAllText(
                    rejectsFilePath,
                    $"TotalActualMWh ne sme biti negativan.,{sample.Date};{sample.TotalActualMWh};{sample.TotalForecastMWh};{sample.CountryCode}"
                    + Environment.NewLine
                );

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
                File.AppendAllText(
                    rejectsFilePath,
                    $"TotalForecastMWh ne sme biti negativan.,{sample.Date};{sample.TotalActualMWh};{sample.TotalForecastMWh};{sample.CountryCode}"
                    + Environment.NewLine
                );

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
                File.AppendAllText(
                    rejectsFilePath,
                    $"PeakTime mora pripadati istom danu.,{sample.Date};{sample.PeakTime};{sample.CountryCode}"
                    + Environment.NewLine
                );

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

            actualConsumptions.Add(sample.TotalActualMWh);

            receivedSamples.Add(sample);

            double actualMean = actualConsumptions.Average();

            double outOfBandPct =
                double.Parse(ConfigurationManager.AppSettings["OutOfBandPct"]);

            double lowerBound =
                actualMean * (1 - outOfBandPct / 100);

            double upperBound =
                actualMean * (1 + outOfBandPct / 100);

            if (sample.TotalActualMWh < lowerBound ||
                sample.TotalActualMWh > upperBound)
            {
                OnWarningRaised?.Invoke(
                    this,
                    new WarningEventArgs
                    {
                        WarningType = WarningType.ConsumptionOutOfBand,
                        Message =
                            $"Potrosnja van dozvoljenog opsega za {sample.Date:dd.MM.yyyy}"
                    });

                Console.WriteLine(
                    $"WARNING: Potrosnja van dozvoljenog opsega ({sample.Date:dd.MM.yyyy})");
            }

            int riseWindowDays =
                int.Parse(ConfigurationManager.AppSettings["RiseWindowDays"]);

            double riseThreshold =
                double.Parse(ConfigurationManager.AppSettings["RiseThresholdMWh"]);

            if (receivedSamples.Count >= riseWindowDays)
            {
                bool rising = true;

                for (int i = receivedSamples.Count - riseWindowDays + 1;
                     i < receivedSamples.Count;
                     i++)
                {
                    double diff =
                        receivedSamples[i].TotalActualMWh -
                        receivedSamples[i - 1].TotalActualMWh;

                    if (diff <= riseThreshold)
                    {
                        rising = false;
                        break;
                    }
                }

                if (rising)
                {
                    OnWarningRaised?.Invoke(
                        this,
                        new WarningEventArgs
                        {
                            WarningType = WarningType.ConsumptionRise,
                            Message =
                                $"Detektovan uzlazni trend potrosnje za {sample.Date:dd.MM.yyyy}"
                        });

                    Console.WriteLine(
                        $"WARNING: Uzlazni trend potrosnje ({sample.Date:dd.MM.yyyy})");
                }
            }

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