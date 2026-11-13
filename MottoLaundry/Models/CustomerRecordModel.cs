using System;
using System.Collections.Generic;
using System.Text;

namespace MottoLaundry.Models
{
    public class CustomerRecordModel
    {
        public string TrackingNumber { get; set; }
        public string CustomerName { get; set; }
        public string ServiceCode { get; set; }
        public double Load { get; set; }

    }
}
