using Microsoft.AspNetCore.Mvc;
using DCBStore.Data;
using DCBStore.Models;
using DCBStore.Helpers;
using System.Linq;

namespace DCBStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "Cart";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Action hiển thị trang giỏ hàng (không thay đổi)
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            return View(cart);
        }

        // Action thêm sản phẩm vào giỏ - ĐÃ ĐƯỢC CẬP NHẬT
        // Action này sẽ được gọi bằng JavaScript (Fetch API)
        [HttpPost] // Chỉ cho phép gọi bằng phương thức POST
        public IActionResult AddToCart(int productId)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                // Trả về lỗi 404 với thông báo dạng JSON
                return NotFound(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            // Lấy giỏ hàng từ session
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();

            var cartItem = cart.FirstOrDefault(item => item.ProductId == productId);

            if (cartItem == null)
            {
                // Nếu sản phẩm chưa có trong giỏ, thêm mới
                cart.Add(new CartItem { ProductId = productId, ProductName = product.Name, Price = product.Price, Quantity = 1 });
            }
            else
            {
                // Nếu đã có, tăng số lượng
                cartItem.Quantity++;
            }

            // Lưu lại giỏ hàng vào session
            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);

            // Tính toán tổng số lượng sản phẩm trong giỏ hàng để trả về cho client
            var cartItemCount = cart.Sum(item => item.Quantity);

            // Trả về kết quả thành công dưới dạng JSON, không chuyển hướng trang
            return Ok(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng!", newCartCount = cartItemCount });
        }
    }
}