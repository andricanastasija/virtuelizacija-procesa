using System;
using System.ServiceModel;
using EnergyConsumptionService.Contracts;
using EnergyConsumptionService.Faults;
using EnergyConsumptionService.Models;
using System.IO;

namespace EnergyConsumptionService.Services
{
    public class EnergyConsumptionServiceImpl : IEnergyConsumptionService
    {
        private string sessionFilePath;
        public void StartSession(SessionMeta meta)
        {
            Console.WriteLine("Session started:");
            Console.WriteLine($"Country: {meta.CountryCode}");
            Console.WriteLine($"Month: {meta.YearMonth}");

            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", meta.CountryCode, meta.YearMonth);

            Directory.CreateDirectory(folderPath);

            sessionFilePath = Path.Combine(folderPath, "session.csv");

            if (!File.Exists(sessionFilePath))
            {
                File.WriteAllText(sessionFilePath,
                    "Date,TotalActualMWh,TotalForecastMWh,PeakTime,PeakActualMW,CountryCode" + Environment.NewLine);
            }
        }

        public void PushSample(DailyConsumptionSample sample)
        {
            if (sample.TotalActualMWh < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "TotalActualMWh ne sme biti negativan.",
                        Field = "TotalActualMWh"
                    });
            }

            if (sample.TotalForecastMWh < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "TotalForecastMWh ne sme biti negativan.",
                        Field = "TotalForecastMWh"
                    });
            }



            if (sample.PeakTime.Date != sample.Date.Date)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "PeakTime mora pripadati istom danu.",
                        Field = "PeakTime"
                    });
            }

            Console.WriteLine($"Sample received for {sample.Date.ToShortDateString()}");

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
            Console.WriteLine("Session ended.");
        }
    }
}