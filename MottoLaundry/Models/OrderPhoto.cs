using System;
using System.Collections.Generic;
using System.Text;

namespace MottoLaundry.Models
{
    class OrderPhoto
    {
        public int _PhotoID { get; set; }

        //  OrderID already in Orders Model

        public string _PhotoPath { get; set; }
        public DateTime _UpdatedAt { get; set; }
    }
}
