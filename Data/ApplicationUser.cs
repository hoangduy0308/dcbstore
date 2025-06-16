using Microsoft.AspNetCore.Identity;

namespace DCBStore.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
    }
}