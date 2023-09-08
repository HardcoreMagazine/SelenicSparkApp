#nullable disable

using Microsoft.AspNetCore.Identity;

namespace SelenicSparkApp.Views.Admin
{
    public class RolesModel
    {
        public IQueryable<IdentityRole> Roles { get; set; }
		public string CreateRoleName { get; set; }
		public string ValidateUserId { get; set; }
		public string AddRoleToUserId { get; set; }
		public string RemoveRoleFromUserId { get; set; }
	}
}
