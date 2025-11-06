using Microsoft.AspNetCore.Mvc;

namespace Client_Datting.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

