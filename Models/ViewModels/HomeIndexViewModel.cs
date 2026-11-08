using SalefnyMVC.Models.Entities;

namespace SalefnyMVC.ViewModels
{
    public class HomeIndexViewModel
    {
        public IEnumerable<Item> FeaturedItems { get; set; } = new List<Item>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public string SearchKeyword { get; set; }
        public string SelectedCity { get; set; }
    }
}