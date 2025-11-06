using Microsoft.AspNetCore.Mvc;

namespace Client_Datting.Controllers
{
    public class CustomerController : Controller
    {
        public IActionResult Discover()
        {
            return View();
        }

        public IActionResult Matches()
        {
            return View();
        }

        public IActionResult Messages()
        {
            return View();
        }

        public IActionResult Photos()
        {
            return View();
        }

        public IActionResult Interests()
        {
            return View();
        }

        public IActionResult BlockedUsers()
        {
            return View();
        }
    }
}

