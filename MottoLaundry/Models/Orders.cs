using System;
using System.Collections.Generic;
using System.Text;

namespace MottoLaundry.Models
{
    class Orders
    {
        public int _OrderID { get; set; }
        public string _TrackingNumber { get; set; }

        //  CustomerID already in Customer Model
        //  ServiceID already in Service Model
        public int _LoadQuantity { get; set; }
        public DateTime _IntakeDate { get; set; }
        public DateTime _DueDate { get; set; }
        public DateTime _DispatchedDate { get; set; }
        public string _ServiceStatus { get; set; }




    }
}
