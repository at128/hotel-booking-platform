using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Domain.Bookings.Enums
{
    public enum PaymentMethod
    {
        Stripe,
        PayPal,
        BankTransfer,
        CreditCard
    }

}
