using Microsoft.AspNetCore.Mvc;

namespace DotNet_Bookstore.Controllers
{
    public class CategoriesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Browse(string category)
        {
            ViewBag.Category = category;
            return View();
        }
    }
}
