using DotNet_Bookstore.Data;
using DotNet_Bookstore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        // POST: /Shop/AddToCart
        [HttpPost]
        public IActionResult AddToCart(int BookId, int Quantity)
        {
            // get book - to access the current book price
            var book = _context.Books.Find(BookId);

            // check if this cart already has this book 
            var cartItem = _context.CartItems.SingleOrDefault(c => c.BookId == BookId &&
                c.CustomerId == GetCustomerId());

            if (cartItem == null)
            {
                // create a new CartItem
                cartItem = new CartItem
                {
                    BookId = BookId,
                    Quantity = Quantity,
                    Price = book.Price,
                    CustomerId = GetCustomerId()
                };

                _context.Add(cartItem);
            }
            // user already has this book in cart - update the quantity
            else
            {
                cartItem.Quantity += Quantity;
                _context.Update(cartItem);
            }

            _context.SaveChanges();

            // display the cart page
            return RedirectToAction("Cart");
        }

        // identify customer for each shopping cart
        private string GetCustomerId()
        {
            // is CustomerId session var already set?
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                // if user is logged in, use their email as session var
                if (User.Identity.IsAuthenticated)
                {
                    // use user's email address as CustomerId
                    HttpContext.Session.SetString("CustomerId", User.Identity.Name);
                }
                else
                {
                    // create new session var using a GUID
                    HttpContext.Session.SetString("CustomerId", Guid.NewGuid().ToString());

                }
            }

            // return the session var so we can identify this user's cart
            return HttpContext.Session.GetString("CustomerId");
        }

        // GET: /Shop/Cart
        public IActionResult Cart()
        {
            // get current customer to filter CartItems query to fetch & display
            var customerId = GetCustomerId();

            // get current cart items and parent Book objects for current customer - JOIN to parent Book to get Book details
            var cartItems = _context.CartItems
                .Include(c => c.Book).OrderBy(c => c.Book.Title)
                .Where(c => c.CustomerId == customerId).ToList();

            // count total of items in cart - for navbar display & store in Session var
            var itemCount = (from c in cartItems
                             select c.Quantity).Sum();
            HttpContext.Session.SetInt32("ItemCount", itemCount);

            return View(cartItems);
        }

        // GET: /Shop/RemoveFromCart/6
        public IActionResult RemoveFromCart(int id)
        {
            // find the item for deletion
            var cartItem = _context.CartItems.Find(id);

            // delete
            _context.CartItems.Remove(cartItem);
            _context.SaveChanges();

            // refresh and display cart
            return RedirectToAction("Cart");
        }

        // GET: /Shop/Ceckout | display an empty checkout form to get customer info
        public IActionResult Checkout()
        {
            return View();
        }

        // POST: /Shop/Checkout
        // capture form data (order containing customer info) and save customer info in a session var
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout([Bind("FirstName,LastName,Address,City,Province,PostalCode,Phone")] Order order)
        {
            // fill the CustomerId and OrderDate
            order.OrderDate = DateTime.Now;
            order.CustomerId = User.Identity.Name;

            // calculate the OrderTotal and set this property
            var cartItems = _context.CartItems.Where(c => c.CustomerId == GetCustomerId());
            order.OrderTotal = (from c in cartItems
                                select (c.Quantity * c.Price)).Sum();

            // save oredr object to session var - after successful payment, retrieve the oreder object from the session var and save it to db
            HttpContext.Session.SetObject("Order", order);


            // redirect to stripe payment
            return RedirectToAction("Payment");
        }


    }
}
