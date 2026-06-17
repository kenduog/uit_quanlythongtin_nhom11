using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom11.Data;
using Nhom11.Models;
using Nhom11.Models.ViewModels;
using Nhom11.Services;
using System.Diagnostics;

namespace Nhom11.Controllers
{
    public class HomeController : Controller
    {
        private readonly DoAnNhom11Context _db;

        public HomeController(DoAnNhom11Context db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var homNay = DateOnly.FromDateTime(DateTime.Today);

            var vm = new DashboardViewModel
            {
                TongDauSach = await _db.Dausaches.CountAsync(),
                TongCuonSach = await _db.Cuonsaches.CountAsync(),
                TongDocGia = await _db.Docgia.CountAsync(),
                SachDangMuon = await _db.Cuonsaches.CountAsync(c => c.TrangThaiKho == PhieuMuonService.TrangThaiDaChoMuon),
                DocGiaBiKhoa = await _db.Docgia.CountAsync(d => d.TrangThai == PhieuMuonService.DocGiaBiKhoa),
                TongNo = await _db.Docgia.SumAsync(d => d.TongNo ?? 0)
            };

            // Các phiếu còn cuốn chưa trả.
            var phieuChuaTra = await _db.Phieumuons
                .Include(p => p.MaDocGiaNavigation)
                .Include(p => p.ChitietPms)
                .Where(p => p.ChitietPms.Any(ct => ct.NgayTraThucTe == null))
                .ToListAsync();

            vm.SachQuaHan = phieuChuaTra
                .Where(p => p.HanPhaiTra < homNay)
                .Sum(p => p.ChitietPms.Count(ct => ct.NgayTraThucTe == null));

            vm.PhieuCanChuY = phieuChuaTra
                .Select(p => new PhieuQuaHanItem
                {
                    MaPhieuMuon = p.MaPhieuMuon,
                    HoTenDocGia = p.MaDocGiaNavigation.HoTen,
                    HanPhaiTra = p.HanPhaiTra,
                    SoCuonChuaTra = p.ChitietPms.Count(ct => ct.NgayTraThucTe == null),
                    SoNgayTre = Math.Max(0, homNay.DayNumber - p.HanPhaiTra.DayNumber)
                })
                .OrderByDescending(x => x.SoNgayTre)
                .ThenBy(x => x.HanPhaiTra)
                .Take(10)
                .ToList();

            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
