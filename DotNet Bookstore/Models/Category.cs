namespace DotNet_Bookstore.Models
{
    public class Category
    {
        // pk fields should always be called either {Model}Id or just Id
        public int CtegoryId { get; set; }
        public string Name { get; set; }
    }
}