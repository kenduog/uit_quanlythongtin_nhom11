using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom11.Data;
using Nhom11.Models.Entities;
using Nhom11.Models.ViewModels;
using Nhom11.Services;

namespace Nhom11.Controllers;

public class DauSachController : Controller
{
    private const int PageSize = 10;
    private readonly DoAnNhom11Context _db;
    private readonly MaGenerator _maGen;

    public DauSachController(DoAnNhom11Context db, MaGenerator maGen)
    {
        _db = db;
        _maGen = maGen;
    }

    // GET: /DauSach
    public async Task<IActionResult> Index(string? tuKhoa, string? theLoai, int page = 1)
    {
        var query = _db.Dausaches.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            query = query.Where(d => d.TenSach.Contains(tuKhoa) || d.TacGia.Contains(tuKhoa) || d.MaDauSach.Contains(tuKhoa));
        }
        if (!string.IsNullOrWhiteSpace(theLoai))
        {
            query = query.Where(d => d.TheLoai == theLoai);
        }

        int total = await query.CountAsync();
        var items = await query
            .OrderBy(d => d.MaDauSach)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.TuKhoa = tuKhoa;
        ViewBag.TheLoai = theLoai;
        ViewBag.DanhSachTheLoai = await _db.Dausaches.Select(d => d.TheLoai).Distinct().OrderBy(t => t).ToListAsync();

        return View(new PagedResult<Dausach>
        {
            Items = items,
            PageNumber = page,
            PageSize = PageSize,
            TotalItems = total
        });
    }

    // GET: /DauSach/Details/DS01
    public async Task<IActionResult> Details(string id)
    {
        var dauSach = await _db.Dausaches
            .Include(d => d.Cuonsaches)
            .FirstOrDefaultAsync(d => d.MaDauSach == id);
        if (dauSach == null) return NotFound();
        return View(dauSach);
    }

    // GET: /DauSach/Create
    public IActionResult Create() => View(new Dausach());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Dausach model)
    {
        ModelState.Remove(nameof(model.MaDauSach)); // Khóa sinh tự động, không có trong form.
        if (!ModelState.IsValid) return View(model);

        model.MaDauSach = await _maGen.SinhMaDauSachAsync();
        _db.Dausaches.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm đầu sách {model.MaDauSach} - {model.TenSach}.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /DauSach/Edit/DS01
    public async Task<IActionResult> Edit(string id)
    {
        var dauSach = await _db.Dausaches.FindAsync(id);
        if (dauSach == null) return NotFound();
        return View(dauSach);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Dausach model)
    {
        if (id != model.MaDauSach) return NotFound();
        if (!ModelState.IsValid) return View(model);

        _db.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật đầu sách {model.MaDauSach}.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /DauSach/Delete/DS01
    public async Task<IActionResult> Delete(string id)
    {
        var dauSach = await _db.Dausaches
            .Include(d => d.Cuonsaches)
            .FirstOrDefaultAsync(d => d.MaDauSach == id);
        if (dauSach == null) return NotFound();
        return View(dauSach);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var dauSach = await _db.Dausaches.Include(d => d.Cuonsaches).FirstOrDefaultAsync(d => d.MaDauSach == id);
        if (dauSach == null) return NotFound();

        if (dauSach.Cuonsaches.Any())
        {
            TempData["Error"] = $"Không thể xóa đầu sách {id} vì còn {dauSach.Cuonsaches.Count} cuốn sách thuộc đầu sách này.";
            return RedirectToAction(nameof(Index));
        }

        _db.Dausaches.Remove(dauSach);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa đầu sách {id}.";
        return RedirectToAction(nameof(Index));
    }
}
