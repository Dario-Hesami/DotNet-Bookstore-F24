using DotNet_Bookstore.Models;
using Microsoft.AspNetCore.Mvc;

namespace DotNet_Bookstore.Controllers
{
    public class CategoriesController : Controller
    {
        public IActionResult Index()
        {
            // use the Category model to generate 10 categories in memory for display in the view
            var categories = new List<Category>();
            for (var i=1; i<11; i++)
            {
                categories.Add(new Category { CategoryId = i, Name = "Category " + i.ToString() });
            }

            return View(categories);
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

        public IActionResult Create()
        {
            return View();
        }
    }
}
