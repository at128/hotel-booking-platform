using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Bookings.Enums
{
    public enum PaymentStatus
    {
        Pending = 0,
        InitiationFailed = 1,
        Succeeded = 2,
        Failed = 3,
        Refunded = 4,
        PartiallyRefunded = 5
    }

}
