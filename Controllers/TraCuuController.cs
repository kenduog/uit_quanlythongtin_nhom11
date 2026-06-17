using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom11.Data;
using Nhom11.Models.ViewModels;
using Nhom11.Services;

namespace Nhom11.Controllers;

public class TraCuuController : Controller
{
    private readonly DoAnNhom11Context _db;

    public TraCuuController(DoAnNhom11Context db)
    {
        _db = db;
    }

    // GET: /TraCuu - tra cứu sách theo tên/tác giả/thể loại + tình trạng còn trong kho.
    public async Task<IActionResult> Index(string? tuKhoa, bool chiConTrongKho = false)
    {
        ViewBag.TuKhoa = tuKhoa;
        ViewBag.ChiConTrongKho = chiConTrongKho;

        if (string.IsNullOrWhiteSpace(tuKhoa))
            return View(new List<KetQuaTraCuuSach>());

        var query = _db.Cuonsaches
            .Include(c => c.MaDauSachNavigation)
            .Where(c => c.MaDauSachNavigation.TenSach.Contains(tuKhoa)
                     || c.MaDauSachNavigation.TacGia.Contains(tuKhoa)
                     || c.MaDauSachNavigation.TheLoai.Contains(tuKhoa));

        if (chiConTrongKho)
            query = query.Where(c => c.TrangThaiKho == PhieuMuonService.TrangThaiConTrongKho);

        var ketQua = await query
            .OrderBy(c => c.MaDauSach).ThenBy(c => c.MaCuonSach)
            .Select(c => new KetQuaTraCuuSach
            {
                MaCuonSach = c.MaCuonSach,
                TenSach = c.MaDauSachNavigation.TenSach,
                TacGia = c.MaDauSachNavigation.TacGia,
                TheLoai = c.MaDauSachNavigation.TheLoai,
                TrangThaiKho = c.TrangThaiKho,
                HienTrangSach = c.HienTrangSach
            })
            .ToListAsync();

        return View(ketQua);
    }
}
