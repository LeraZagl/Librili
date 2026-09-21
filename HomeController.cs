using Microsoft.AspNetCore.Mvc;

namespace MyLibrary.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Error() => View();
    }
}
