using System.ComponentModel.DataAnnotations;

namespace Final.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Stock { get; set; }
        public int TotalStock { get; set; }
        public string Image { get; set; }
        public string Description { get; set; }
        public string Screen { get; set; }
        public string OS { get; set; }
        public string Camera { get; set; }
        public string RAM { get; set; }
        public string Storage { get; set; }
        public string Warranty { get; set; }

        public decimal? DiscountPercentage
        {
            get
            {
                if (DiscountPrice.HasValue && DiscountPrice < Price)
                {
                    return Math.Round(100 - (DiscountPrice.Value / Price * 100), 1);
                }
                return null;
            }
        }
    }
}