using System.ServiceModel;
using EnergyConsumptionClient.Models;

namespace EnergyConsumptionClient.Contracts
{
    [ServiceContract(Namespace = "http://energyconsumption/service")]
    public interface IEnergyConsumptionService
    {
        [OperationContract]
        void StartSession(SessionMeta meta);

        [OperationContract]
        void PushSample(DailyConsumptionSample sample);

        [OperationContract]
        void EndSession();
    }
}