using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnergyConsumptionService.Events
{
    public class WarningEventArgs : EventArgs
    {
        public WarningType WarningType { get; set; }

        public string Message { get; set; }
    }
}
