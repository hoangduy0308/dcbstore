using DCBStore.Data;
using DCBStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);
            var users = await _context.Users
                                      .Where(u => u.Id != currentUserId)
                                      .ToListAsync();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                ViewData[$"UserRoles_{user.Id}"] = string.Join(", ", roles);
            }

            return View(users);
        }

        // GET: Admin/Users/Create
        public IActionResult Create()
        {
            ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");
            return View();
        }

        // POST: Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.Role);
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);

                ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.Role);
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.Role))
            {
                if (await _roleManager.RoleExistsAsync(model.Role))
                    await _userManager.AddToRoleAsync(user, model.Role);
                else
                {
                    ModelState.AddModelError("", $"Vai trò '{model.Role}' không tồn tại.");
                    ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.Role);
                    return View(model);
                }
            }

            TempData["SuccessMessage"] = "Người dùng đã được tạo thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = _roleManager.Roles.ToList();

            var vm = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                CurrentRoles = userRoles,
                SelectedRole = userRoles.FirstOrDefault()
            };

            ViewBag.Roles = new SelectList(allRoles, "Name", "Name", vm.SelectedRole);
            return View(vm);
        }

        // POST: Admin/Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.SelectedRole);
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            if (user.Email != model.Email)
            {
                var emailResult = await _userManager.SetEmailAsync(user, model.Email);
                var userNameResult = await _userManager.SetUserNameAsync(user, model.Email);
                if (!emailResult.Succeeded || !userNameResult.Succeeded)
                {
                    foreach (var e in emailResult.Errors.Concat(userNameResult.Errors))
                        ModelState.AddModelError("", e.Description);

                    ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.SelectedRole);
                    return View(model);
                }
            }

            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                var removePwd = await _userManager.RemovePasswordAsync(user);
                var addPwd = await _userManager.AddPasswordAsync(user, model.NewPassword);
                if (!removePwd.Succeeded || !addPwd.Succeeded)
                {
                    foreach (var e in removePwd.Errors.Concat(addPwd.Errors))
                        ModelState.AddModelError("", e.Description);

                    ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.SelectedRole);
                    return View(model);
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var e in updateResult.Errors)
                    ModelState.AddModelError("", e.Description);

                ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.SelectedRole);
                return View(model);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!string.IsNullOrEmpty(model.SelectedRole))
                await _userManager.AddToRoleAsync(user, model.SelectedRole);

            TempData["SuccessMessage"] = "Người dùng đã được cập nhật thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Users/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewData["UserRoles"] = string.Join(", ", roles);
            return View(user);
        }

        // POST: Admin/Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["ErrorMessage"] = "Bạn không thể xóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            // 1) Xóa chat messages
            var chats = await _context.ChatMessages
                .Where(m => m.SenderId == id || m.ReceiverId == id)
                .ToListAsync();
            _context.ChatMessages.RemoveRange(chats);

            // 2) Xóa reviews
            var reviews = await _context.Reviews
                .Where(r => r.UserId == id)
                .ToListAsync();
            _context.Reviews.RemoveRange(reviews);

            // 3) Xóa order details và orders
            var userOrders = await _context.Orders
                .Where(o => o.UserId == id)
                .Include(o => o.OrderDetails)
                .ToListAsync();
            var orderDetails = userOrders.SelectMany(o => o.OrderDetails).ToList();
            _context.OrderDetails.RemoveRange(orderDetails);
            _context.Orders.RemoveRange(userOrders);

            // Lưu các thay đổi trước
            await _context.SaveChangesAsync();

            // 4) Xóa user
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Người dùng đã được xóa thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa người dùng: " +
                    string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewData["UserRoles"] = string.Join(", ", roles);
            return View(user);
        }

        // POST: Admin/Users/LockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["ErrorMessage"] = "Bạn không thể khóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            var lockoutEnd = DateTimeOffset.UtcNow.AddYears(999);
            var res = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);
            TempData[res.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                res.Succeeded
                    ? $"Người dùng '{user.Email}' đã được khóa."
                    : "Khóa tài khoản thất bại: " + string.Join("; ", res.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Users/UnlockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var res = await _userManager.SetLockoutEndDateAsync(user, null);
            TempData[res.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                res.Succeeded
                    ? $"Người dùng '{user.Email}' đã được mở khóa."
                    : "Mở khóa thất bại: " + string.Join("; ", res.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Index));
        }
    }
}
