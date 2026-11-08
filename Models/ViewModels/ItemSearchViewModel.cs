using SalefnyMVC.Models.Entities;

namespace SalefnyMVC.ViewModels
{
    public class ItemSearchViewModel
    {
        public string Keyword { get; set; }
        public string City { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public IEnumerable<Item> Items { get; set; } = new List<Item>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
    }
}