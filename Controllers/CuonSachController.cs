using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nhom11.Data;
using Nhom11.Models.Entities;
using Nhom11.Models.ViewModels;
using Nhom11.Services;

namespace Nhom11.Controllers;

public class CuonSachController : Controller
{
    private const int PageSize = 10;
    private readonly DoAnNhom11Context _db;
    private readonly MaGenerator _maGen;

    public CuonSachController(DoAnNhom11Context db, MaGenerator maGen)
    {
        _db = db;
        _maGen = maGen;
    }

    // GET: /CuonSach
    public async Task<IActionResult> Index(string? maDauSach, string? trangThaiKho, int page = 1)
    {
        var query = _db.Cuonsaches.Include(c => c.MaDauSachNavigation).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(maDauSach))
            query = query.Where(c => c.MaDauSach == maDauSach);
        if (!string.IsNullOrWhiteSpace(trangThaiKho))
            query = query.Where(c => c.TrangThaiKho == trangThaiKho);

        int total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.MaCuonSach)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.MaDauSach = maDauSach;
        ViewBag.TrangThaiKho = trangThaiKho;
        await NapDropdownAsync(maDauSach);

        return View(new PagedResult<Cuonsach>
        {
            Items = items,
            PageNumber = page,
            PageSize = PageSize,
            TotalItems = total
        });
    }

    // GET: /CuonSach/Details/CS01_01
    public async Task<IActionResult> Details(string id)
    {
        var cuon = await _db.Cuonsaches
            .Include(c => c.MaDauSachNavigation)
            .Include(c => c.ChitietPms).ThenInclude(ct => ct.MaPhieuMuonNavigation)
            .FirstOrDefaultAsync(c => c.MaCuonSach == id);
        if (cuon == null) return NotFound();
        return View(cuon);
    }

    // GET: /CuonSach/Create
    public async Task<IActionResult> Create()
    {
        await NapDropdownAsync(null);
        return View(new Cuonsach { TrangThaiKho = PhieuMuonService.TrangThaiConTrongKho, HienTrangSach = "Mới" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Cuonsach model)
    {
        ModelState.Remove(nameof(model.MaCuonSach));          // Khóa sinh tự động.
        ModelState.Remove(nameof(model.MaDauSachNavigation)); // Navigation không bind từ form.
        if (!ModelState.IsValid)
        {
            await NapDropdownAsync(model.MaDauSach);
            return View(model);
        }

        model.MaCuonSach = await _maGen.SinhMaCuonSachAsync(model.MaDauSach);
        _db.Cuonsaches.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm cuốn sách {model.MaCuonSach}.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /CuonSach/Edit/CS01_01
    public async Task<IActionResult> Edit(string id)
    {
        var cuon = await _db.Cuonsaches.FindAsync(id);
        if (cuon == null) return NotFound();
        await NapDropdownAsync(cuon.MaDauSach);
        return View(cuon);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Cuonsach model)
    {
        if (id != model.MaCuonSach) return NotFound();
        ModelState.Remove(nameof(model.MaDauSachNavigation)); // Navigation không bind từ form.
        if (!ModelState.IsValid)
        {
            await NapDropdownAsync(model.MaDauSach);
            return View(model);
        }

        _db.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật cuốn sách {model.MaCuonSach}.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /CuonSach/Delete/CS01_01
    public async Task<IActionResult> Delete(string id)
    {
        var cuon = await _db.Cuonsaches.Include(c => c.MaDauSachNavigation).FirstOrDefaultAsync(c => c.MaCuonSach == id);
        if (cuon == null) return NotFound();
        return View(cuon);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var cuon = await _db.Cuonsaches.Include(c => c.ChitietPms).FirstOrDefaultAsync(c => c.MaCuonSach == id);
        if (cuon == null) return NotFound();

        if (cuon.ChitietPms.Any())
        {
            TempData["Error"] = $"Không thể xóa cuốn {id} vì đã có lịch sử mượn/trả.";
            return RedirectToAction(nameof(Index));
        }

        _db.Cuonsaches.Remove(cuon);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa cuốn sách {id}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task NapDropdownAsync(string? maDauSachChon)
    {
        var dsDauSach = await _db.Dausaches.OrderBy(d => d.MaDauSach)
            .Select(d => new { d.MaDauSach, TenHienThi = d.MaDauSach + " - " + d.TenSach })
            .ToListAsync();
        ViewBag.DauSachList = new SelectList(dsDauSach, "MaDauSach", "TenHienThi", maDauSachChon);
    }
}
