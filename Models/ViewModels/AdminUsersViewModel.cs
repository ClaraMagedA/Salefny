namespace SalefnyMVC.ViewModels
{
    public class AdminUsersViewModel
    {
        public string Search { get; set; } = string.Empty;
        public List<AdminUserRow> Users { get; set; } = new();
    }

    public class AdminUserRow
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new();
        public int ListingsCount { get; set; }
        public int BookingsCount { get; set; }
        public bool IsSuperAdmin { get; set; }
        public bool IsSelf { get; set; }

        public bool CanSuspend => !IsSuperAdmin && !IsSelf;
    }
}
