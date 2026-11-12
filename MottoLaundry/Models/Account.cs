using System;
using System.Collections.Generic;
using System.Text;

namespace MottoLaundry.Models
{
    class Account
    {
        public int _AccountID { get; set; }
        public string _AccountUsername { get; set; }
        public string _PasswordHash { get; set; }

        //  DepartmentID already in Department
    }
}
