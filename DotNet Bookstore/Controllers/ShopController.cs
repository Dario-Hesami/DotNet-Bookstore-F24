using DotNet_Bookstore.Data;
using Microsoft.AspNetCore.Mvc;

namespace DotNet_Bookstore.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShopController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            // query the list of categories in a-z order for display
            var categories = _context.Categories.OrderBy(c => c.Name).ToList();
            return View(categories);
        }

        // GET: /Shop/ShopByCategory/6 

        public IActionResult ShopByCategory(int id)

        {

            // Display the category name on the page based on the ID - store category name in the ViewData object 
            var category = _context.Categories.Find(id);

            // return to /shop/index if category not found 
            if (category == null)
            {
                return RedirectToAction("Index");
            }
            ViewData["Category"] = category.Name;

            // query the Books filtered by the selected CategoryId param 
            var books = _context.Books.Where(b => b.CategoryId == id)
                .OrderBy(b => b.Title)
                .ToList();

            // send the Books list to the view for display 
            return View(books);
        }
    }
}
