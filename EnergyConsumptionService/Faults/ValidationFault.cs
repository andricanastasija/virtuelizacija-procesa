using System.Runtime.Serialization;

namespace EnergyConsumptionService.Faults
{
    [DataContract]
    public class ValidationFault
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string Field { get; set; }
    }
}