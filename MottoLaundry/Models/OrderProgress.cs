using System;
using System.Collections.Generic;
using System.Text;

namespace MottoLaundry.Models
{
    class OrderProgress
    {
        public int _OrderID { get; set; }

        //  StageID already in stage

        public string _StageStatus { get; set; }
        public string _PerformedByEmployeeID { get; set; }
        public DateTime _UpdateAt { get; set; }

    }
}
