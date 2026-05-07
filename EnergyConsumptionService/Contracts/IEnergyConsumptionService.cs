using System.ServiceModel;
using EnergyConsumptionService.Models;
using EnergyConsumptionService.Faults;

namespace EnergyConsumptionService.Contracts
{
    [ServiceContract(Namespace = "http://energyconsumption/service")]
    public interface IEnergyConsumptionService
    {
        [OperationContract]
        void StartSession(SessionMeta meta);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        void PushSample(DailyConsumptionSample sample);

        [OperationContract]
        void EndSession();
    }
}