using System.ComponentModel.DataAnnotations;

namespace DotNet_Bookstore.Models
{
    public class OrderDetail
    {
        //PK

        public int OrderDetailId { get; set; }



        [Required]

        public int Quantity { get; set; }



        [Required]

        public decimal Price { get; set; }



        //FK

        [Required]

        public int OrderId { get; set; }



        //FK

        [Required]

        public int BookId { get; set; }



        // parent references

        public Book? Book { get; set; }

        public Order? Order { get; set; }
    }
}
