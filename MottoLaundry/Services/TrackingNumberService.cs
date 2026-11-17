
using System.Text.RegularExpressions;

namespace MottoLaundry.Services
{
    public class TrackingNumberService
    {
        // Accepts TRK-### only
        public bool isValidFormat(string trackingNumber)
        {
            return !string.IsNullOrWhiteSpace(trackingNumber)
                && Regex.IsMatch(
                    trackingNumber,
                    @"\ATRK-[0-9]{3}\z");
        }
    }
}
