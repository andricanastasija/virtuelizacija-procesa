using System;
using System.Runtime.Serialization;

namespace EnergyConsumptionService.Models
{
    [DataContract(Namespace = "http://energyconsumption/models")]
    public class DailyConsumptionSample
    {
        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public double TotalActualMWh { get; set; }

        [DataMember]
        public double TotalForecastMWh { get; set; }

        [DataMember]
        public DateTime PeakTime { get; set; }

        [DataMember]
        public double PeakActualMW { get; set; }

        [DataMember]
        public string CountryCode { get; set; }

        [DataMember]
        public int RowIndex { get; set; }
    }
}