using DCBStore.Data;
using DCBStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DCBStore.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2)
            {
                return Json(new List<object>());
            }

            var products = await _context.Products
                .Where(p => !p.IsDeleted && p.Name.ToLower().Contains(term.ToLower()))
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    imageUrl = p.Images.FirstOrDefault().Url ?? "/images/placeholder.png"
                })
                .Take(5)
                .ToListAsync();

            return Json(products);
        }

        public async Task<IActionResult> Index(
            string? searchString,
            int? categoryId,
            string? sortOrder,
            decimal? minPrice,
            decimal? maxPrice,
            int? minRating,
            string? status,
            int pageNumber = 1)
        {
            var productsQuery = _context.Products
                                      .Where(p => !p.IsDeleted)
                                      .Include(p => p.Category)
                                      .Include(p => p.Images)
                                      .Include(p => p.Reviews)
                                      .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(s => s.Name.ToLower().Contains(searchString.ToLower()));
            }

            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);
            }

            if (minRating.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Reviews.Any() && p.Reviews.Average(r => r.Rating) >= minRating.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "in-stock")
                {
                    productsQuery = productsQuery.Where(p => p.Stock > 0);
                }
                else if (status == "out-of-stock")
                {
                    productsQuery = productsQuery.Where(p => p.Stock == 0);
                }
            }

            ViewData["CurrentSort"] = sortOrder;
            switch (sortOrder)
            {
                case "price_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Price);
                    break;
                case "price_asc":
                    productsQuery = productsQuery.OrderBy(p => p.Price);
                    break;
                case "best_selling":
                    productsQuery = productsQuery.OrderByDescending(p => p.SoldQuantity);
                    break;
                default:
                    productsQuery = productsQuery.OrderByDescending(p => p.Id);
                    break;
            }

            int pageSize = 8;
            var count = await productsQuery.CountAsync();
            var pagedProducts = await productsQuery
                                      .Skip((pageNumber - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToListAsync();

            var viewModel = new ProductIndexViewModel
            {
                Products = pagedProducts,
                Categories = await _context.Categories.Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToListAsync(),
                SearchString = searchString,
                CategoryId = categoryId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinRating = minRating,
                Status = status,
                SortOrder = sortOrder,
                PageNumber = pageNumber,
                TotalPages = (int)Math.Ceiling(count / (double)pageSize)
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                                      .Where(p => !p.IsDeleted && p.Id == id)
                                      .Include(p => p.Category)
                                      .Include(p => p.Images)
                                      .Include(p => p.Reviews.OrderByDescending(r => r.ReviewDate))
                                        .ThenInclude(r => r.User)
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}