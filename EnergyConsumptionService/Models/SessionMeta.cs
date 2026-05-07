using System.Runtime.Serialization;

namespace EnergyConsumptionService.Models
{
    [DataContract(Namespace = "http://energyconsumption/models")]
    public class SessionMeta
    {
        [DataMember]
        public string CountryCode { get; set; }

        [DataMember]
        public string YearMonth { get; set; }

        [DataMember]
        public string SourceFileName { get; set; }

        [DataMember]
        public int TotalDays { get; set; }
    }
}