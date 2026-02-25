using System.Collections.Generic;

namespace HotelBooking.Domain.Common.Results;

public interface IValidationFailureFactory<TSelf>
{
    static abstract TSelf FromValidationErrors(IReadOnlyCollection<Error> errors);
}