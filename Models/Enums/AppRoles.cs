namespace SalefnyMVC.Models.Enums
{
    public static class AppRoles
    {
        public const string Renter = "Renter";
        public const string Owner = "Owner";
        public const string Admin = "Admin";
        public const string SuperAdmin = "SuperAdmin";

        public static readonly string[] All = { Renter, Owner, Admin, SuperAdmin };
    }
}
