using Microsoft.EntityFrameworkCore;
using DCBStore.Data;
using Microsoft.AspNetCore.Identity; // Thêm dòng này để sử dụng Identity

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
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 4. Đăng ký các dịch vụ MVC
builder.Services.AddControllersWithViews();
// Thêm dịch vụ cho Razor Pages (cần cho các trang của Identity)
builder.Services.AddRazorPages();


// --- Kết thúc vùng cấu hình dịch vụ ---

var app = builder.Build();

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
app.UseSession(); // Kích hoạt Session
app.UseAuthentication(); // Bật Xác thực (Xác định bạn là ai)
app.UseAuthorization(); // Bật Phân quyền (Kiểm tra bạn được làm gì)


// Cấu hình các endpoint
app.MapRazorPages(); // Thêm dòng này để các trang của Identity hoạt động
// Route cho các Area
app.MapControllerRoute(
  name: "MyArea",
  pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

// Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();