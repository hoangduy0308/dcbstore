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
using System.Collections.Generic; 

namespace DCBStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
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
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser 
                { 
                    UserName = model.Email, 
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    EmailConfirmed = true 
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        var roleExists = await _roleManager.RoleExistsAsync(model.Role);
                        if (roleExists)
                        {
                            await _userManager.AddToRoleAsync(user, model.Role);
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, $"Vai trò '{model.Role}' không tồn tại. Vui lòng chọn vai trò hợp lệ.");
                            ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.Role);
                            return View(model);
                        }
                    }
                    TempData["SuccessMessage"] = "Người dùng đã được tạo thành công.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.Role);
            return View(model);
        }

        // GET: Admin/Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = _roleManager.Roles.ToList();

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                CurrentRoles = userRoles,
                SelectedRole = userRoles.FirstOrDefault() 
            };

            ViewBag.Roles = new SelectList(allRoles, "Name", "Name", viewModel.SelectedRole);
            return View(viewModel);
        }

        // POST: Admin/Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return NotFound();
                }

                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;

                if (user.Email != model.Email)
                {
                    var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                    if (!setEmailResult.Succeeded)
                    {
                        foreach (var error in setEmailResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.SelectedRole);
                        return View(model);
                    }
                    var setUserNameResult = await _userManager.SetUserNameAsync(user, model.Email);
                    if (!setUserNameResult.Succeeded)
                    {
                        foreach (var error in setUserNameResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.SelectedRole);
                        return View(model);
                    }
                }

                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    var removePasswordResult = await _userManager.RemovePasswordAsync(user);
                    if (!removePasswordResult.Succeeded)
                    {
                        foreach (var error in removePasswordResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.SelectedRole);
                        return View(model);
                    }

                    var addPasswordResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
                    if (!addPasswordResult.Succeeded)
                    {
                        foreach (var error in addPasswordResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.SelectedRole);
                        return View(model);
                    }
                }

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.SelectedRole);
                    return View(model);
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeRolesResult.Succeeded)
                {
                    foreach (var error in removeRolesResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.SelectedRole);
                    return View(model);
                }

                if (!string.IsNullOrEmpty(model.SelectedRole))
                {
                    var addRoleResult = await _userManager.AddToRoleAsync(user, model.SelectedRole);
                    if (!addRoleResult.Succeeded)
                    {
                        foreach (var error in addRoleResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.SelectedRole);
                        return View(model);
                    }
                }

                TempData["SuccessMessage"] = "Người dùng đã được cập nhật thành công.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", model.SelectedRole);
            return View(model);
        }

        // GET: Admin/Users/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            ViewData["UserRoles"] = string.Join(", ", userRoles);

            return View(user);
        }

        // POST: Admin/Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["ErrorMessage"] = "Bạn không thể xóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Người dùng đã được xóa thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa người dùng: " + string.Join("; ", result.Errors.Select(e => e.Description));
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            ViewData["UserRoles"] = string.Join(", ", userRoles);

            return View(user);
        }

        // POST: Admin/Users/LockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["ErrorMessage"] = "Bạn không thể khóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            // Khóa tài khoản vĩnh viễn (hoặc một thời gian dài)
            var lockoutEndDate = DateTimeOffset.UtcNow.AddYears(999); 
            var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEndDate);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Người dùng '{user.Email}' đã được khóa thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi khóa người dùng: " + string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Users/UnlockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Mở khóa tài khoản bằng cách đặt thời gian khóa là null (ngay lập tức)
            var result = await _userManager.SetLockoutEndDateAsync(user, null);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Người dùng '{user.Email}' đã được mở khóa thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi mở khóa người dùng: " + string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}