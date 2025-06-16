using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace DCBStore.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider service)
        {
            // Lấy các dịch vụ cần thiết
            var userManager = service.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = service.GetRequiredService<RoleManager<IdentityRole>>();

            // --- TẠO CÁC VAI TRÒ (ROLES) ---
            // Kiểm tra và tạo vai trò "Admin" nếu chưa có
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Kiểm tra và tạo vai trò "User" nếu chưa có
            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }

            // --- TẠO TÀI KHOẢN ADMIN MẶC ĐỊNH ---

            // Vui lòng thay đổi thông tin đăng nhập và mật khẩu tại đây
            string adminEmail = "admin@dcbstore.com";
            string adminPassword = "Admin@123";

            // Kiểm tra xem tài khoản admin đã tồn tại chưa
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                // Nếu chưa có, tạo mới
                var newAdminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true // Tự động xác thực email
                };

                var result = await userManager.CreateAsync(newAdminUser, adminPassword);

                if (result.Succeeded)
                {
                    // Gán vai trò "Admin" cho tài khoản vừa tạo
                    await userManager.AddToRoleAsync(newAdminUser, "Admin");
                }
            }
        }
    }
}
