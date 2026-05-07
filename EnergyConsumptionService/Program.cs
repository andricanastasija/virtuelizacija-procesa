using System;
using System.ServiceModel;
using EnergyConsumptionService.Services;

namespace EnergyConsumptionService
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Uri baseAddress = new Uri("net.tcp://localhost:9000/EnergyConsumptionService");

            using (ServiceHost host = new ServiceHost(typeof(EnergyConsumptionServiceImpl), baseAddress))
            {
                host.Open();

                Console.WriteLine("Energy Consumption WCF Service is running...");
                Console.WriteLine("Address: net.tcp://localhost:9000/EnergyConsumptionService");
                Console.WriteLine("Press ENTER to stop service.");

                Console.ReadLine();

                host.Close();
            }
        }
    }
}