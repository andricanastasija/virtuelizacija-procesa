using System.Runtime.Serialization;

namespace EnergyConsumptionService.Faults
{
    [DataContract]
    public class DataFormatFault
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string Details { get; set; }
    }
}