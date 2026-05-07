using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using EnergyConsumptionClient.Contracts;
using EnergyConsumptionClient.Models;
using EnergyConsumptionClient.Services;

namespace EnergyConsumptionClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            NetTcpBinding binding = new NetTcpBinding();
            EndpointAddress address = new EndpointAddress("net.tcp://localhost:9000/EnergyConsumptionService");

            ChannelFactory<IEnergyConsumptionService> factory =
                new ChannelFactory<IEnergyConsumptionService>(binding, address);

            IEnergyConsumptionService proxy = factory.CreateChannel();

            SessionMeta meta = new SessionMeta
            {
                CountryCode = "GB_GBN",
                YearMonth = "2015-01",
                SourceFileName = "time_series_30min_singleindex.csv",
                TotalDays = 1
            };

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            @"..\..\CSV\time_series_30min_singleindex (1).csv");

            CsvReaderService csvReader = new CsvReaderService();

            List<DailyConsumptionSample> samples =
                csvReader.ReadDailySamples(filePath, "GB_GBN", "2015-01");


            try
            {
                proxy.StartSession(meta);

                foreach (var sample in samples)
                {
                    proxy.PushSample(sample);
                    Console.WriteLine($"Sent sample for {sample.Date.ToShortDateString()}");
                }

                proxy.EndSession();

                ((IClientChannel)proxy).Close();
                factory.Close();

                Console.WriteLine("Data sent to service.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greska tokom slanja: " + ex.Message);

                ((IClientChannel)proxy).Abort();
                factory.Abort();
            }
            finally
            {
                Console.WriteLine("Resursi su zatvoreni.");
            }

            
            Console.ReadLine();
        }
    }
}