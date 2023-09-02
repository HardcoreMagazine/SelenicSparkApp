using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SelenicSparkApp.Models
{
    /// <summary>
    /// Expands IdentityUser fields, binded to IdentityUser migration-database; DOES NOT inherit the original class.
    /// </summary>
    public class IdentityUserExpander
    {
        [Key]
        public string UID { get; set; }

        public IdentityUser User { get; set; }

        public int UsernameChangeTokens { get; set; }

        public int UserWarningsCount { get; set; }

        public int UserLockoutCount { get; set; }

        public IdentityUserExpander()
        {
            // Default constructor. Delete this and all migration builds will start failing.
        }
        public IdentityUserExpander(string userId, IdentityUser identityUser, int tokens = 1, int warnings = 0, int bans = 0)
        {
            UID = userId;
            User = identityUser;
            UsernameChangeTokens = tokens;
            UserWarningsCount = warnings;
            UserLockoutCount = bans;
        }
    }
}
