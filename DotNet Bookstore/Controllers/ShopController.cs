using DotNet_Bookstore.Data;
using DotNet_Bookstore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace DotNet_Bookstore.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        // class level conig object to read Stripe SecretKey from appsettings.json
        private readonly IConfiguration _configuration;

        public ShopController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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

        // GET: /Shop/Payment
        // invoke Stripe payment page & response
        [Authorize]

        public IActionResult Payment()
        {
            // get the order from our session var
            var order = HttpContext.Session.GetObject<Order>("Order");

            // read Stripe SecretKey from app configuration (appsettings.json) using _configuration class var
            StripeConfiguration.ApiKey = _configuration.GetValue<string>("StripeSecretKey");
            //StripeConfiguration.ApiKey = "sk_test_51OFKOwHXWyWx1WwQXKhXUBJVrJ8BSYNo1AaHQ4q5855GHCJRFKSCbbyqPZlOqEtvCrEJliEaWYKLMRK87TzXVxvc00KqVZQu6Z";

            // source: https://stripe.com/docs/checkout/quickstart and modified

            // get domain dynamically (local or live)
            var domain = "https://" + Request.Host;

            // create Stripe payment object
            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                  new SessionLineItemOptions
                  {
                      PriceData = new SessionLineItemPriceDataOptions
                      {
                          UnitAmount = (long?)(order.OrderTotal * 100), // amount in smalles currency unit (e.g., cents)
                          Currency = "cad",
                          ProductData = new SessionLineItemPriceDataProductDataOptions
                          {
                              Name = "DotNetBookstore Purchase"
                          }
                      },
                    Quantity = 1,
                  },
                },
                Mode = "payment",
                SuccessUrl = domain + "/Shop/SaveOrder",
                CancelUrl = domain + "/Shop/Cart",
            };

            // invoke Stripe with the above payment object
            var service = new SessionService();
            Session session = service.Create(options);

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        // GET: /Shop/SaveOrder
        // once the payment is made, save the order and then show the confirmation
        [Authorize]
        public IActionResult SaveOrder()
        {
            // the user's order already has been saved in a session variable (an object)
            // get the order from session var
            var order = HttpContext.Session.GetObject<Order>("Order");

            // save the order to db
            _context.Orders.Add(order);
            _context.SaveChanges();

            // save the user's cart items to db-OrderDetail 
            var cartItems = _context.CartItems.Where(c => c.CustomerId == GetCustomerId());
            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    BookId = item.BookId,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    OrderId = order.OrderId
                };

                _context.OrderDetails.Add(orderDetail);
            }
            _context.SaveChanges();

            // remove the user's cart items
            foreach (var item in cartItems)
            {
                _context.CartItems.Remove(item);
            }
            _context.SaveChanges();

            // clear all session vars
            HttpContext.Session.Clear();

            // redirect to  the order confirmation - /Orders/Details/5
            return RedirectToAction("Details", "Orders", new { @id = order.OrderId });
        }



    }
}
