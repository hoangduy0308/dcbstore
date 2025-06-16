using Microsoft.EntityFrameworkCore;
using DCBStore.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication; 
var builder = WebApplication.CreateBuilder(args);

// --- Bắt đầu vùng cấu hình dịch vụ ---

// 1. Cấu hình kết nối Cơ sở dữ liệu
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Cấu hình dịch vụ Session cho Giỏ hàng
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3. Cấu hình dịch vụ Identity cho Tài khoản người dùng
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 4. Cấu hình các dịch vụ xác thực bên ngoài (Google)
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        // Code này sẽ tự động đọc ClientId và ClientSecret từ user-secrets
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    });

// Cấu hình lại đường dẫn đăng nhập mặc định cho Admin
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Admin/Account/Login";
    options.AccessDeniedPath = "/Admin/Account/AccessDenied";
});

// 5. Đăng ký các dịch vụ MVC
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// --- Kết thúc vùng cấu hình dịch vụ ---

var app = builder.Build();

// Tự động tạo Vai trò và tài khoản Admin khi ứng dụng chạy
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbSeeder.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Quan trọng: Phải đặt các middleware này theo đúng thứ tự
app.UseSession(); 
app.UseAuthentication(); 
app.UseAuthorization(); 

// Cấu hình các endpoint
app.MapRazorPages();
app.MapControllerRoute(
  name: "MyArea",
  pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
