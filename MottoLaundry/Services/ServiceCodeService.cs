
using System;
using System.Collections.Generic;

namespace MottoLaundry.Services
{
    public class ServiceCodeService
    {
        private readonly HashSet<string> _serviceCode =
            new(StringComparer.OrdinalIgnoreCase);

        public void LoadServiceCode(IEnumerable<string> codes)
        {
            _serviceCode.Clear();

            foreach (string code in codes)
            {
                if (!string.IsNullOrWhiteSpace(code))
                {
                    _serviceCode.Add(code.Trim());
                }
            }
        }

        public bool IsValidServiceCode(string serviceCode)
        {
            return !string.IsNullOrWhiteSpace(serviceCode)
                && _serviceCode.Contains(serviceCode.Trim());
        }
    }
}
