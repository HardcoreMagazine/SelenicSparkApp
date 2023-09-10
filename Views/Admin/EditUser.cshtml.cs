namespace SelenicSparkApp.Views.Admin
{
    public class EditUserModel
    {
        // IdentityUser
        public required string Id { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required bool EmailConfirmed { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public required int AccessFailedCount { get; set; }
        // IdentityUserExpander
        public required int UsernameChangeTokens { get; set; }
        public required int UserWarningsCount { get; set; }
        // Hidden fields
        public required HashSet<string> UserRoles { get; set; }
        public required HashSet<string> AvailableRoles { get; set; } // Roles which user don't have
    }
}
