namespace SalefnyMVC.Models.ViewModels
{
    public class AdminManagementViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsSuperAdmin { get; set; }
        public bool IsCurrentUser { get; set; }
    }
}
