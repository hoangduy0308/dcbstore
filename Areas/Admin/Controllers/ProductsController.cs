using DCBStore.Data;
using DCBStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DCBStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // --- CÁC PHƯƠNG THỨC INDEX, DETAILS, CREATE (GET) GIỮ NGUYÊN ---
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                                         .Include(p => p.Category)
                                         .Include(p => p.Images)
                                         .ToListAsync();
            return View(products);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,Stock,CategoryId")] Product product, List<IFormFile> imageFiles)
        {
            ModelState.Remove("Category");
            ModelState.Remove("Images");
            
            if (ModelState.IsValid)
            {
                if (imageFiles != null && imageFiles.Count > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    
                    foreach (var imageFile in imageFiles)
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        var productImage = new ProductImage
                        {
                            Url = "/images/products/" + uniqueFileName,
                        };
                        product.Images.Add(productImage);
                    }
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sản phẩm đã được tạo thành công.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products
                                        .Include(p => p.Images)
                                        .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // ====> POST: Admin/Products/Edit/5 - ĐÃ VIẾT LẠI HOÀN TOÀN <====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Stock,CategoryId")] Product productFromForm, List<IFormFile> newImageFiles)
        {
            if (id != productFromForm.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Bước 1: Lấy sản phẩm gốc từ database, không theo dõi nó vội
                var productToUpdate = await _context.Products.FindAsync(id);

                if (productToUpdate == null)
                {
                    return NotFound();
                }

                // Bước 2: Cập nhật các thuộc tính của sản phẩm gốc từ dữ liệu của form
                productToUpdate.Name = productFromForm.Name;
                productToUpdate.Description = productFromForm.Description;
                productToUpdate.Price = productFromForm.Price;
                productToUpdate.Stock = productFromForm.Stock;
                productToUpdate.CategoryId = productFromForm.CategoryId;
                
                try
                {
                    // Bước 3: Xử lý thêm ảnh mới (nếu có)
                    if (newImageFiles != null && newImageFiles.Count > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                        foreach (var imageFile in newImageFiles)
                        {
                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await imageFile.CopyToAsync(fileStream);
                            }
                            // Thêm ảnh mới vào DB với ProductId chính xác
                            _context.ProductImages.Add(new ProductImage { Url = "/images/products/" + uniqueFileName, ProductId = id });
                        }
                    }

                    // Bước 4: Lưu tất cả thay đổi (cả thông tin sản phẩm và ảnh mới)
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Sản phẩm đã được cập nhật thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.Id == productToUpdate.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            // Nếu model không hợp lệ, tải lại trang với dữ liệu đã có
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", productFromForm.CategoryId);
            return View(productFromForm);
        }

        // --- CÁC PHƯƠNG THỨC DELETE, DELETEIMAGE GIỮ NGUYÊN ---
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
            if (product != null)
            {
                foreach (var image in product.Images)
                {
                    if (!string.IsNullOrEmpty(image.Url))
                    {
                        var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.Url.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }
                }
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sản phẩm đã được xóa thành công.";
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<JsonResult> DeleteImage(int id)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image == null)
            {
                return Json(new { success = false, message = "Không tìm thấy ảnh." });
            }
            if (!string.IsNullOrEmpty(image.Url))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.Url.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }
            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Ảnh đã được xóa." });
        }
    }
}