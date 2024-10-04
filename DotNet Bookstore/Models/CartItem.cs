using System.ComponentModel.DataAnnotations;

namespace DotNet_Bookstore.Models
{
    public class CartItem
    {
        //PK

        public int CartItemId { get; set; }



        [Required]

        public int Quantity { get; set; }



        [Required]

        public decimal Price { get; set; }



        [Required]

        public string CustomerId { get; set; }



        //FK

        [Required]

        public int BookId { get; set; }



        // parent reference

        public Book? Book { get; set; }

    }
}
