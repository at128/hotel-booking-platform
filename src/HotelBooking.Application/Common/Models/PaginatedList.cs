using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Application.Common.Models
{
    public class PaginatedList<T>
    {
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalPages { get; init; }
        public int TotalCount { get; init; }

        public IReadOnlyCollection<T> Items { get; init; } = [];
    }
}
