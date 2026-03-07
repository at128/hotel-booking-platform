using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Controllers
{
    public class AdminHotelRoomTypesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
