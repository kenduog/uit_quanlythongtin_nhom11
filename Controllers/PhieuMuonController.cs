using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nhom11.Data;
using Nhom11.Models.Entities;
using Nhom11.Models.ViewModels;
using Nhom11.Services;

namespace Nhom11.Controllers;

public class PhieuMuonController : Controller
{
    private const int PageSize = 10;
    private readonly DoAnNhom11Context _db;
    private readonly IPhieuMuonService _service;
    private readonly ThuVienOptions _opt;

    public PhieuMuonController(DoAnNhom11Context db, IPhieuMuonService service, IOptions<ThuVienOptions> opt)
    {
        _db = db;
        _service = service;
        _opt = opt.Value;
    }

    // GET: /PhieuMuon
    public async Task<IActionResult> Index(string? tuKhoa, string? trangThai, int page = 1)
    {
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        var query = _db.Phieumuons
            .Include(p => p.MaDocGiaNavigation)
            .Include(p => p.ChitietPms)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(tuKhoa))
            query = query.Where(p => p.MaPhieuMuon.Contains(tuKhoa) || p.MaDocGiaNavigation.HoTen.Contains(tuKhoa) || p.MaDocGia.Contains(tuKhoa));

        // Lọc theo trạng thái trả (tính phía client sau khi tải vì cần đếm chi tiết).
        var all = await query.OrderByDescending(p => p.NgayMuon).ThenByDescending(p => p.MaPhieuMuon).ToListAsync();

        IEnumerable<Phieumuon> loc = all;
        if (trangThai == "chuatra")
            loc = all.Where(p => p.ChitietPms.Any(ct => ct.NgayTraThucTe == null));
        else if (trangThai == "datra")
            loc = all.Where(p => p.ChitietPms.All(ct => ct.NgayTraThucTe != null));
        else if (trangThai == "quahan")
            loc = all.Where(p => p.HanPhaiTra < homNay && p.ChitietPms.Any(ct => ct.NgayTraThucTe == null));

        var locList = loc.ToList();
        int total = locList.Count;
        var items = locList.Skip((page - 1) * PageSize).Take(PageSize).ToList();

        ViewBag.TuKhoa = tuKhoa;
        ViewBag.TrangThai = trangThai;
        ViewBag.HomNay = homNay;

        return View(new PagedResult<Phieumuon>
        {
            Items = items,
            PageNumber = page,
            PageSize = PageSize,
            TotalItems = total
        });
    }

    // GET: /PhieuMuon/Details/PM001
    public async Task<IActionResult> Details(string id)
    {
        var phieu = await _db.Phieumuons
            .Include(p => p.MaDocGiaNavigation)
            .Include(p => p.ChitietPms).ThenInclude(ct => ct.MaCuonSachNavigation).ThenInclude(c => c.MaDauSachNavigation)
            .FirstOrDefaultAsync(p => p.MaPhieuMuon == id);
        if (phieu == null) return NotFound();
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        return View(phieu);
    }

    // GET: /PhieuMuon/Create - màn hình lập phiếu mượn
    public async Task<IActionResult> Create()
    {
        var vm = new LapPhieuMuonViewModel
        {
            HanPhaiTra = DateOnly.FromDateTime(DateTime.Today.AddDays(_opt.SoNgayMuonMacDinh))
        };
        await NapNguonDuLieuAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LapPhieuMuonViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await NapNguonDuLieuAsync(vm);
            return View(vm);
        }

        var ketQua = await _service.LapPhieuMuonAsync(vm.MaDocGia, vm.DsMaCuonSach ?? new(), vm.HanPhaiTra);
        if (!ketQua.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, ketQua.ThongDiep);
            await NapNguonDuLieuAsync(vm);
            return View(vm);
        }

        TempData["Success"] = ketQua.ThongDiep;
        return RedirectToAction(nameof(Details), new { id = ketQua.MaThamChieu });
    }

    // GET: /PhieuMuon/TraSach/PM001 - màn hình trả sách
    public async Task<IActionResult> TraSach(string id)
    {
        var phieu = await _db.Phieumuons
            .Include(p => p.MaDocGiaNavigation)
            .Include(p => p.ChitietPms).ThenInclude(ct => ct.MaCuonSachNavigation).ThenInclude(c => c.MaDauSachNavigation)
            .FirstOrDefaultAsync(p => p.MaPhieuMuon == id);
        if (phieu == null) return NotFound();

        var vm = new TraSachViewModel
        {
            Phieu = phieu,
            CacDong = phieu.ChitietPms.Select(ct => new DongTraSachViewModel
            {
                MaCuonSach = ct.MaCuonSach,
                TenSach = ct.MaCuonSachNavigation?.MaDauSachNavigation?.TenSach ?? ct.MaCuonSach,
                DaTra = ct.NgayTraThucTe != null,
                NgayTraThucTe = ct.NgayTraThucTe,
                TienPhatHienTai = ct.TienPhat,
                HienTrangKhiTra = "Bình thường"
            }).ToList()
        };
        return View(vm);
    }

    [HttpPost, ActionName("TraSach")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TraSachConfirmed(string id, TraSachViewModel vm)
    {
        var dongTra = (vm.CacDong ?? new())
            .Where(d => d.ChonTra)
            .Select(d => new DongTraSach(d.MaCuonSach, d.HienTrangKhiTra))
            .ToList();

        var ketQua = await _service.TraSachAsync(id, dongTra);
        if (!ketQua.ThanhCong)
        {
            TempData["Error"] = ketQua.ThongDiep;
            return RedirectToAction(nameof(TraSach), new { id });
        }

        TempData["Success"] = ketQua.ThongDiep;
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task NapNguonDuLieuAsync(LapPhieuMuonViewModel vm)
    {
        var homNay = DateOnly.FromDateTime(DateTime.Today);

        // Chỉ liệt kê độc giả đủ điều kiện mượn (không khóa, còn hạn).
        var docGiaItems = await _db.Docgia
            .Where(d => d.TrangThai != PhieuMuonService.DocGiaBiKhoa && d.NgayHetHan >= homNay)
            .OrderBy(d => d.MaDocGia)
            .Select(d => new { d.MaDocGia, TenHienThi = d.MaDocGia + " - " + d.HoTen })
            .ToListAsync();
        vm.DocGiaList = new SelectList(docGiaItems, "MaDocGia", "TenHienThi", vm.MaDocGia);

        // Cuốn sách còn trong kho.
        vm.CuonSachConTrongKho = await _db.Cuonsaches
            .Where(c => c.TrangThaiKho == PhieuMuonService.TrangThaiConTrongKho)
            .Include(c => c.MaDauSachNavigation)
            .OrderBy(c => c.MaCuonSach)
            .Select(c => new CuonSachConTrongKho(
                c.MaCuonSach,
                c.MaDauSachNavigation.TenSach,
                c.MaDauSachNavigation.TacGia,
                c.HienTrangSach))
            .ToListAsync();
    }
}
