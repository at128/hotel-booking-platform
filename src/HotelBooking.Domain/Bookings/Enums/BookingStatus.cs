using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Bookings.Enums
{
    public enum BookingStatus
    {
        Pending = 0,
        Confirmed = 1,
        CheckedIn = 2,
        Completed = 3,
        Cancelled = 4,
        Failed = 5
    }
}
