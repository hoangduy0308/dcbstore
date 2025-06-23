using DCBStore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DCBStore.ViewComponents
{
    public class CategoriesViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CategoriesViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        // Phương thức InvokeAsync sẽ được gọi khi bạn render component
        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Lấy danh sách danh mục chưa bị "xóa mềm"
            var categories = await _context.Categories
                                           .Where(c => !c.IsDeleted)
                                           .OrderBy(c => c.Name) // Sắp xếp theo tên cho gọn
                                           .ToListAsync();
            return View(categories); // Trả về view mặc định với model là danh sách categories
        }
    }
}