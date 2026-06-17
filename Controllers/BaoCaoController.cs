using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom11.Data;
using Nhom11.Models.ViewModels;
using Nhom11.Services;

namespace Nhom11.Controllers;

public class BaoCaoController : Controller
{
    private readonly DoAnNhom11Context _db;

    public BaoCaoController(DoAnNhom11Context db)
    {
        _db = db;
    }

    // GET: /BaoCao
    public async Task<IActionResult> Index(DateOnly? tuNgay, DateOnly? denNgay)
    {
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        var vm = new BaoCaoViewModel
        {
            TuNgay = tuNgay ?? DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)),
            DenNgay = denNgay ?? homNay
        };

        // 1. Sách đang được mượn (chi tiết chưa trả).
        vm.SachDangMuon = await _db.ChitietPms
            .Where(ct => ct.NgayTraThucTe == null)
            .Include(ct => ct.MaPhieuMuonNavigation).ThenInclude(p => p.MaDocGiaNavigation)
            .Include(ct => ct.MaCuonSachNavigation).ThenInclude(c => c.MaDauSachNavigation)
            .Select(ct => new SachDangMuonItem
            {
                MaCuonSach = ct.MaCuonSach,
                TenSach = ct.MaCuonSachNavigation.MaDauSachNavigation.TenSach,
                HoTenDocGia = ct.MaPhieuMuonNavigation.MaDocGiaNavigation.HoTen,
                MaPhieuMuon = ct.MaPhieuMuon,
                HanPhaiTra = ct.MaPhieuMuonNavigation.HanPhaiTra
            })
            .OrderBy(x => x.HanPhaiTra)
            .ToListAsync();

        // 2. Phiếu quá hạn chưa trả.
        var phieuChuaTra = await _db.Phieumuons
            .Include(p => p.MaDocGiaNavigation)
            .Include(p => p.ChitietPms)
            .Where(p => p.HanPhaiTra < homNay && p.ChitietPms.Any(ct => ct.NgayTraThucTe == null))
            .ToListAsync();
        vm.PhieuQuaHan = phieuChuaTra
            .Select(p => new PhieuQuaHanItem
            {
                MaPhieuMuon = p.MaPhieuMuon,
                HoTenDocGia = p.MaDocGiaNavigation.HoTen,
                HanPhaiTra = p.HanPhaiTra,
                SoCuonChuaTra = p.ChitietPms.Count(ct => ct.NgayTraThucTe == null),
                SoNgayTre = homNay.DayNumber - p.HanPhaiTra.DayNumber
            })
            .OrderByDescending(x => x.SoNgayTre)
            .ToList();

        // 3. Độc giả còn nợ.
        vm.DocGiaCoNo = await _db.Docgia
            .Where(d => (d.TongNo ?? 0) > 0)
            .OrderByDescending(d => d.TongNo)
            .Select(d => new DocGiaNoItem
            {
                MaDocGia = d.MaDocGia,
                HoTen = d.HoTen,
                TongNo = d.TongNo ?? 0,
                TrangThai = d.TrangThai
            })
            .ToListAsync();

        // 4. Sách được mượn nhiều nhất.
        vm.SachMuonNhieu = await _db.ChitietPms
            .Include(ct => ct.MaCuonSachNavigation).ThenInclude(c => c.MaDauSachNavigation)
            .GroupBy(ct => new { ct.MaCuonSachNavigation.MaDauSachNavigation.TenSach, ct.MaCuonSachNavigation.MaDauSachNavigation.TacGia })
            .Select(g => new SachMuonNhieuItem
            {
                TenSach = g.Key.TenSach,
                TacGia = g.Key.TacGia,
                SoLuotMuon = g.Count()
            })
            .OrderByDescending(x => x.SoLuotMuon)
            .Take(10)
            .ToListAsync();

        // 5. Tổng tiền phạt phát sinh trong kỳ (theo ngày trả thực tế).
        vm.TongTienPhatTrongKy = await _db.ChitietPms
            .Where(ct => ct.NgayTraThucTe != null
                      && ct.NgayTraThucTe >= vm.TuNgay
                      && ct.NgayTraThucTe <= vm.DenNgay)
            .SumAsync(ct => ct.TienPhat ?? 0);

        return View(vm);
    }
}
