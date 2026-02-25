using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotelBooking.Application.Common.Interfaces;

public interface ICacheInvalidator
{
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByTagAsync(string tag, CancellationToken ct = default);
    Task RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken ct = default);
}