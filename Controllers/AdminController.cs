using Eticaret.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Eticaret.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // Admin girişi için kontrol
        private bool IsAdmin()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return false;

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            return user != null && user.Role == "Admin";
        }

        // Admin giriş kontrolü yapan action filter
        private IActionResult CheckAdmin()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }
            return null;
        }

        // GET: Admin/Dashboard
        public IActionResult Dashboard()
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            ViewBag.ProductCount = _context.Products.Count();
            ViewBag.CategoryCount = _context.Categories.Count();
            ViewBag.UserCount = _context.Users.Count();
            ViewBag.OrderCount = _context.Orders.Count();

            var recentOrders = _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList();

            return View(recentOrders);
        }

        #region Ürün Yönetimi

        // GET: Admin/Products
        public async Task<IActionResult> Products()
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            var products = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            return View(products);
        }

        // GET: Admin/ProductCreate
        public IActionResult ProductCreate()
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            var categories = _context.Categories.ToList();
            if (!categories.Any())
            {
                TempData["ErrorMessage"] = "Önce kategori eklemelisiniz.";
                return RedirectToAction(nameof(Categories));
            }

            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(new Product());
        }

        // POST: Admin/ProductCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProductCreate([Bind("Name,CategoryId,Price,Stock,Description")] Product product, IFormFile Image)
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
            {
                System.Diagnostics.Debug.WriteLine("Admin kontrolü başarısız");
                return checkResult;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"Form verisi: CategoryId={Request.Form["CategoryId"]}, Name={product.Name}, Price={product.Price}");

                // Kategori kontrolü
                var categoryId = 0;
                if (int.TryParse(Request.Form["CategoryId"], out categoryId))
                {
                    product.CategoryId = categoryId;
                }

                if (product.CategoryId <= 0)
                {
                    System.Diagnostics.Debug.WriteLine("Kategori seçilmedi");
                    ModelState.AddModelError("CategoryId", "Lütfen bir kategori seçin");
                }

                // ModelState'i temizle ve sadece gerekli alanları kontrol et
                ModelState.Clear();
                if (string.IsNullOrEmpty(product.Name))
                    ModelState.AddModelError("Name", "Ürün adı zorunludur");
                if (product.Price <= 0)
                    ModelState.AddModelError("Price", "Fiyat 0'dan büyük olmalıdır");
                if (product.Stock < 0)
                    ModelState.AddModelError("Stock", "Stok miktarı 0 veya daha büyük olmalıdır");
                if (product.CategoryId <= 0)
                    ModelState.AddModelError("CategoryId", "Lütfen bir kategori seçin");

                var category = await _context.Categories.FindAsync(product.CategoryId);
                if (category == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Kategori bulunamadı: {product.CategoryId}");
                    ModelState.AddModelError("CategoryId", "Seçilen kategori bulunamadı");
                }

                if (ModelState.IsValid && category != null)
                {
                    product.CreatedDate = DateTime.Now;

                    System.Diagnostics.Debug.WriteLine($"Ürün bilgileri: Name={product.Name}, CategoryId={product.CategoryId}, Price={product.Price}, Stock={product.Stock}");

                    // Dosya yükleme işlemi
                    if (Image != null && Image.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "products");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Image.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await Image.CopyToAsync(fileStream);
                        }

                        product.ImageUrl = "/images/products/" + uniqueFileName;
                        System.Diagnostics.Debug.WriteLine($"Resim yüklendi: {product.ImageUrl}");
                    }

                    await _context.Products.AddAsync(product);
                    await _context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine("Ürün başarıyla kaydedildi");
                    TempData["SuccessMessage"] = "Ürün başarıyla oluşturuldu.";
                    return RedirectToAction(nameof(Products));
                }
                else
                {
                    foreach (var modelState in ModelState.Values)
                    {
                        foreach (var error in modelState.Errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"Validation Error: {error.ErrorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hata oluştu: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                ModelState.AddModelError("", "Ürün kaydedilirken bir hata oluştu: " + ex.Message);
            }

            // Hata durumunda kategori listesini tekrar yükle
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Admin/ProductEdit/5
        public async Task<IActionResult> ProductEdit(int? id)
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Admin/ProductEdit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProductEdit(int id, Product product)
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = await _context.Products.FindAsync(id);

                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.Stock = product.Stock;
                    existingProduct.CategoryId = product.CategoryId;

                    // Dosya yükleme işlemi
                    if (Request.Form.Files.Count > 0)
                    {
                        var file = Request.Form.Files.FirstOrDefault();
                        if (file != null && file.Length > 0)
                        {
                            // Eski resmi sil
                            if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                            {
                                var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, existingProduct.ImageUrl.TrimStart('/'));
                                if (System.IO.File.Exists(oldImagePath))
                                {
                                    System.IO.File.Delete(oldImagePath);
                                }
                            }

                            var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "products");
                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }

                            existingProduct.ImageUrl = "/images/products/" + uniqueFileName;
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Products));
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Admin/ProductDelete/5
        public async Task<IActionResult> ProductDelete(int? id)
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Admin/ProductDelete/5
        [HttpPost, ActionName("ProductDelete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProductDeleteConfirmed(int id)
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            var product = await _context.Products.FindAsync(id);

            // Ürün resmini sil
            if (product != null && !string.IsNullOrEmpty(product.ImageUrl))
            {
                var imagePath = Path.Combine(_hostEnvironment.WebRootPath, product.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Products));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        #endregion

        #region Kategori Yönetimi

        // GET: Admin/Categories
        public async Task<IActionResult> Categories()
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            return View(await _context.Categories.ToListAsync());
        }

        // GET: Admin/CategoryCreate
        public IActionResult CategoryCreate()
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            return View();
        }

        // POST: Admin/CategoryCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CategoryCreate(Category category)
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            try
            {
                if (ModelState.IsValid)
                {
                    category.Products = new List<Product>(); // Initialize empty Products collection
                    _context.Categories.Add(category); // Use Add instead of _context.Add
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Kategori başarıyla oluşturuldu.";
                    return RedirectToAction(nameof(Categories));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Kategori kaydedilirken bir hata oluştu: " + ex.Message);
            }
            return View(category);
        }

        // GET: Admin/CategoryEdit/5
        public async Task<IActionResult> CategoryEdit(int? id)
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Admin/CategoryEdit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CategoryEdit(int id, Category category)
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Categories));
            }
            return View(category);
        }

        // GET: Admin/CategoryDelete/5
        public async Task<IActionResult> CategoryDelete(int? id)
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Admin/CategoryDelete/5
        [HttpPost, ActionName("CategoryDelete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CategoryDeleteConfirmed(int id)
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            var category = await _context.Categories.FindAsync(id);
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Categories));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }

        #endregion

        #region Sipariş Yönetimi

        // GET: Admin/Orders
        public async Task<IActionResult> Orders()
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            var orders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // GET: Admin/OrderDetails/5
        public async Task<IActionResult> OrderDetails(int? id)
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Admin/UpdateOrderStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(OrderDetails), new { id = id });
        }

        #endregion

        #region Kullanıcı Yönetimi

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            return View(await _context.Users.ToListAsync());
        }

        // POST: Admin/UpdateUserRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(int userId, string role)
        {
            var checkResult = CheckAdmin();
            if (checkResult != null)
                return checkResult;

            if (string.IsNullOrEmpty(role))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest("Rol boş olamaz");
                }
                else
                {
                    return BadRequest("Rol boş olamaz");
                }
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return NotFound("Kullanıcı bulunamadı");
                }
                else
                {
                    return NotFound();
                }
            }

            // Rol değişikliğini uygula
            user.Role = role;
            await _context.SaveChangesAsync();

            // AJAX isteği ise JSON yanıt dön
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Kullanıcı rolü başarıyla güncellendi" });
            }

            return RedirectToAction(nameof(Users));
        }

        #endregion
    }
}