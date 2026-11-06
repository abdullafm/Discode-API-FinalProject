namespace Domain.Constants
{
    public static class Roles
    {
        public const string Member = "Member";
        public const string Admin = "Admin";
        public const string SuperAdmin = "SuperAdmin";
        public const string Staff = SuperAdmin + "," + Admin;

        public static readonly string[] All =
        {
            Member,
            Admin,
            SuperAdmin
        };
    }
}
