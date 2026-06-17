using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom11.Data;
using Nhom11.Models.Entities;
using Nhom11.Models.ViewModels;
using Nhom11.Services;

namespace Nhom11.Controllers;

public class DocGiaController : Controller
{
    private const int PageSize = 10;
    private readonly DoAnNhom11Context _db;
    private readonly MaGenerator _maGen;

    public DocGiaController(DoAnNhom11Context db, MaGenerator maGen)
    {
        _db = db;
        _maGen = maGen;
    }

    // GET: /DocGia
    public async Task<IActionResult> Index(string? tuKhoa, string? trangThai, int page = 1)
    {
        var query = _db.Docgia.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(tuKhoa))
            query = query.Where(d => d.HoTen.Contains(tuKhoa) || d.MaDocGia.Contains(tuKhoa));
        if (!string.IsNullOrWhiteSpace(trangThai))
            query = query.Where(d => d.TrangThai == trangThai);

        int total = await query.CountAsync();
        var items = await query
            .OrderBy(d => d.MaDocGia)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.TuKhoa = tuKhoa;
        ViewBag.TrangThai = trangThai;
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);

        return View(new PagedResult<Docgium>
        {
            Items = items,
            PageNumber = page,
            PageSize = PageSize,
            TotalItems = total
        });
    }

    // GET: /DocGia/Details/DG001
    public async Task<IActionResult> Details(string id)
    {
        var docGia = await _db.Docgia
            .Include(d => d.Phieumuons).ThenInclude(p => p.ChitietPms)
            .FirstOrDefaultAsync(d => d.MaDocGia == id);
        if (docGia == null) return NotFound();
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        return View(docGia);
    }

    // GET: /DocGia/Create
    public IActionResult Create() => View(new Docgium
    {
        NgayLapThe = DateOnly.FromDateTime(DateTime.Today),
        NgayHetHan = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
        TongNo = 0,
        TrangThai = PhieuMuonService.DocGiaBinhThuong
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Docgium model)
    {
        if (model.NgayHetHan <= (model.NgayLapThe ?? DateOnly.FromDateTime(DateTime.Today)))
            ModelState.AddModelError(nameof(model.NgayHetHan), "Ngày hết hạn phải lớn hơn ngày lập thẻ.");
        if (!ModelState.IsValid) return View(model);

        model.MaDocGia = await _maGen.SinhMaDocGiaAsync();
        model.TongNo ??= 0;
        model.TrangThai ??= PhieuMuonService.DocGiaBinhThuong;
        _db.Docgia.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm độc giả {model.MaDocGia} - {model.HoTen}.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /DocGia/Edit/DG001
    public async Task<IActionResult> Edit(string id)
    {
        var docGia = await _db.Docgia.FindAsync(id);
        if (docGia == null) return NotFound();
        return View(docGia);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Docgium model)
    {
        if (id != model.MaDocGia) return NotFound();
        if (model.NgayHetHan <= (model.NgayLapThe ?? DateOnly.FromDateTime(DateTime.Today)))
            ModelState.AddModelError(nameof(model.NgayHetHan), "Ngày hết hạn phải lớn hơn ngày lập thẻ.");
        if (!ModelState.IsValid) return View(model);

        _db.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật độc giả {model.MaDocGia}.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /DocGia/DoiTrangThai - khóa / mở khóa thẻ
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(string id)
    {
        var docGia = await _db.Docgia.FindAsync(id);
        if (docGia == null) return NotFound();

        docGia.TrangThai = docGia.TrangThai == PhieuMuonService.DocGiaBiKhoa
            ? PhieuMuonService.DocGiaBinhThuong
            : PhieuMuonService.DocGiaBiKhoa;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã chuyển thẻ {id} sang trạng thái '{docGia.TrangThai}'.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /DocGia/Delete/DG001
    public async Task<IActionResult> Delete(string id)
    {
        var docGia = await _db.Docgia.Include(d => d.Phieumuons).FirstOrDefaultAsync(d => d.MaDocGia == id);
        if (docGia == null) return NotFound();
        return View(docGia);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var docGia = await _db.Docgia.Include(d => d.Phieumuons).FirstOrDefaultAsync(d => d.MaDocGia == id);
        if (docGia == null) return NotFound();

        if (docGia.Phieumuons.Any())
        {
            TempData["Error"] = $"Không thể xóa độc giả {id} vì còn lịch sử mượn sách.";
            return RedirectToAction(nameof(Index));
        }

        _db.Docgia.Remove(docGia);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa độc giả {id}.";
        return RedirectToAction(nameof(Index));
    }
}
