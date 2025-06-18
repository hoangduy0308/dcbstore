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

        // SỬA: Lọc bỏ các sản phẩm đã bị "xóa mềm" khỏi danh sách chính
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                                       .Where(p => !p.IsDeleted) // CHỈ HIỂN THỊ SẢN PHẨM CHƯA BỊ XÓA
                                       .Include(p => p.Category)
                                       .Include(p => p.Images)
                                       .OrderByDescending(p => p.Id)
                                       .ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted); // Ngăn xem chi tiết sản phẩm đã xóa
            if (product == null) return NotFound();
            return View(product);
        }
        
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }
        
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

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products
                                      .Include(p => p.Images)
                                      .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null || product.IsDeleted) return NotFound(); // Không cho sửa sản phẩm đã xóa
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Stock,CategoryId,SoldQuantity,IsDeleted")] Product productFromForm, List<IFormFile> newImageFiles, string[] existingImageUrls)
        {
            if (id != productFromForm.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Category");
            ModelState.Remove("Images");

            if (ModelState.IsValid)
            {
                var productToUpdate = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
                if (productToUpdate == null)
                {
                    return NotFound();
                }

                productToUpdate.Name = productFromForm.Name;
                productToUpdate.Description = productFromForm.Description;
                productToUpdate.Price = productFromForm.Price;
                productToUpdate.Stock = productFromForm.Stock;
                productToUpdate.CategoryId = productFromForm.CategoryId;
                productToUpdate.SoldQuantity = productFromForm.SoldQuantity;
                productToUpdate.IsDeleted = productFromForm.IsDeleted; // Thêm dòng này để có thể khôi phục sản phẩm

                try
                {
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
                            _context.ProductImages.Add(new ProductImage { Url = "/images/products/" + uniqueFileName, ProductId = id });
                        }
                    }

                    var imagesToDelete = productToUpdate.Images.Where(img => !existingImageUrls.Contains(img.Url)).ToList();
                    foreach (var imgToDelete in imagesToDelete)
                    {
                        var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, imgToDelete.Url.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                        _context.ProductImages.Remove(imgToDelete);
                    }
                    
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Sản phẩm đã được cập nhật thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Đã có lỗi xảy ra khi cập nhật sản phẩm.";
                    Console.WriteLine(ex.ToString()); // Ghi log lỗi
                }
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", productFromForm.CategoryId);
            return View(productFromForm);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // SỬA: Action này giờ sẽ thực hiện "Xóa mềm"
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // THAY ĐỔI LOGIC TẠI ĐÂY
                // Thay vì xóa, chúng ta chỉ đánh dấu là đã xóa
                product.IsDeleted = true;
                _context.Update(product); // Đánh dấu đối tượng là đã thay đổi
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sản phẩm đã được xóa (ẩn đi) thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm để xóa.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<JsonResult> DeleteImage(int id)
        {
            // ... Logic DeleteImage giữ nguyên ...
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