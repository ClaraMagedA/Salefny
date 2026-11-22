using SalefnyMVC.Models.Entities;

namespace SalefnyMVC.ViewModels
{
    public class ItemSearchViewModel
    {
        // Preserved filter values
        public string Keyword { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string Sort { get; set; } = "newest";

        // Paging
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        // Results and lookup data
        public IEnumerable<Item> Items { get; set; } = new List<Item>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public IEnumerable<string> Cities { get; set; } = new List<string>();

        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
