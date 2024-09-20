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
            // if category empty, redirct to Index method so user can first pick a category
            if (category == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.Category = category;
            return View();
        }
    }
}
