using DCBStore.Data;
using DCBStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using DCBStore.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc.Rendering; 

namespace DCBStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CouponsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CouponsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Coupons
        public async Task<IActionResult> Index()
        {
            var coupons = await _context.Coupons
                                        .Include(c => c.Product)
                                        .Include(c => c.Category)
                                        .ToListAsync();
            return View(coupons);
        }

        // GET: Admin/Coupons/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new CouponViewModel
            {
                Products = new SelectList(await _context.Products.OrderBy(p => p.Name).ToListAsync(), "Id", "Name"),
                Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name")
            };
            return View(viewModel);
        }

        // POST: Admin/Coupons/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CouponViewModel model)
        {
            if (ModelState.IsValid)
            {
                var coupon = new Coupon
                {
                    Code = model.Code,
                    DiscountType = model.DiscountType,
                    Value = model.Value,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    MaxUses = model.MaxUses,
                    MinOrderAmount = model.MinOrderAmount,
                    ProductId = model.ProductId,
                    CategoryId = model.CategoryId,
                    OneTimePerUser = model.OneTimePerUser
                };

                // Kiểm tra xung đột logic
                if (coupon.DiscountType == DiscountType.Percentage && (coupon.Value <= 0 || coupon.Value > 100))
                {
                    ModelState.AddModelError(nameof(model.Value), "Giá trị phần trăm phải từ 1 đến 100.");
                }

                if (model.ProductId.HasValue && model.CategoryId.HasValue)
                {
                    ModelState.AddModelError(string.Empty, "Mã giảm giá không thể áp dụng cho cả sản phẩm và danh mục cùng lúc.");
                }

                if (ModelState.IsValid)
                {
                    _context.Add(coupon);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Mã giảm giá đã được tạo thành công.";
                    return RedirectToAction(nameof(Index));
                }
            }

            model.Products = new SelectList(await _context.Products.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", model.ProductId);
            model.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", model.CategoryId);
            return View(model);
        }

        // GET: Admin/Coupons/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _context.Coupons
                                       .Include(c => c.Product)
                                       .Include(c => c.Category)
                                       .FirstOrDefaultAsync(m => m.Id == id);
            if (coupon == null)
            {
                return NotFound();
            }

            var viewModel = new CouponViewModel
            {
                Id = coupon.Id,
                Code = coupon.Code,
                DiscountType = coupon.DiscountType,
                Value = coupon.Value,
                StartDate = coupon.StartDate,
                EndDate = coupon.EndDate,
                MaxUses = coupon.MaxUses,
                TimesUsed = coupon.TimesUsed,
                MinOrderAmount = coupon.MinOrderAmount,
                ProductId = coupon.ProductId,
                CategoryId = coupon.CategoryId,
                OneTimePerUser = coupon.OneTimePerUser,
                Products = new SelectList(await _context.Products.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", coupon.ProductId),
                Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", coupon.CategoryId)
            };
            return View(viewModel);
        }

        // POST: Admin/Coupons/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CouponViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var couponToUpdate = await _context.Coupons.FindAsync(id);
                if (couponToUpdate == null)
                {
                    return NotFound();
                }

                // Cập nhật các thuộc tính
                couponToUpdate.Code = model.Code;
                couponToUpdate.DiscountType = model.DiscountType;
                couponToUpdate.Value = model.Value;
                couponToUpdate.StartDate = model.StartDate;
                couponToUpdate.EndDate = model.EndDate;
                couponToUpdate.MaxUses = model.MaxUses;
                couponToUpdate.MinOrderAmount = model.MinOrderAmount;
                couponToUpdate.ProductId = model.ProductId;
                couponToUpdate.CategoryId = model.CategoryId;
                couponToUpdate.OneTimePerUser = model.OneTimePerUser;

                // Kiểm tra xung đột logic (tương tự như Create)
                if (couponToUpdate.DiscountType == DiscountType.Percentage && (couponToUpdate.Value <= 0 || couponToUpdate.Value > 100))
                {
                    ModelState.AddModelError(nameof(model.Value), "Giá trị phần trăm phải từ 1 đến 100.");
                }

                if (model.ProductId.HasValue && model.CategoryId.HasValue)
                {
                    ModelState.AddModelError(string.Empty, "Mã giảm giá không thể áp dụng cho cả sản phẩm và danh mục cùng lúc.");
                }

                if (ModelState.IsValid) // Kiểm tra lại ModelState sau khi thêm lỗi tùy chỉnh
                {
                    try
                    {
                        _context.Update(couponToUpdate);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Mã giảm giá đã được cập nhật thành công.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!_context.Coupons.Any(e => e.Id == id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            // Nếu ModelState không hợp lệ, tải lại dữ liệu cho dropdown và hiển thị lại form
            model.Products = new SelectList(await _context.Products.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", model.ProductId);
            model.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", model.CategoryId);
            return View(model);
        }

        // GET: Admin/Coupons/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _context.Coupons
                                       .Include(c => c.Product)
                                       .Include(c => c.Category)
                                       .FirstOrDefaultAsync(m => m.Id == id);
            if (coupon == null)
            {
                return NotFound();
            }
            return View(coupon);
        }

        // POST: Admin/Coupons/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Mã giảm giá đã được xóa thành công.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}