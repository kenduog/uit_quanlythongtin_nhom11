using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nhom11.Data;
using Nhom11.Models.ViewModels;
using Nhom11.Services;

namespace Nhom11.Controllers;

/// <summary>
/// Demo 5 stored procedure theo bố cục B1–B5. Mọi bảng đều truy vấn trực tiếp SQL Server
/// (qua <see cref="IThuTucService"/>), không lưu dữ liệu trong web.
/// </summary>
public class ThuTucController : Controller
{
    private readonly DoAnNhom11Context _db;
    private readonly IThuTucService _svc;

    public ThuTucController(DoAnNhom11Context db, IThuTucService svc)
    {
        _db = db;
        _svc = svc;
    }

    public IActionResult Index() => View();

    // ===================== 1) SP_LapPhieuMuon =====================
    private static readonly (string, string)[] BangLapPhieu =
    {
        ("PHIEUMUON (mới nhất)", "SELECT TOP 50 MaPhieuMuon, MaDocGia, NgayMuon, HanPhaiTra FROM PHIEUMUON ORDER BY MaPhieuMuon DESC"),
        ("CHITIET_PM (mới nhất)", "SELECT TOP 50 MaPhieuMuon, MaCuonSach, NgayTraThucTe, HienTrangKhiTra, TienPhat FROM CHITIET_PM ORDER BY MaPhieuMuon DESC"),
        ("CUONSACH", "SELECT TOP 50 MaCuonSach, MaDauSach, TrangThaiKho, HienTrangSach FROM CUONSACH ORDER BY MaCuonSach"),
    };

    [HttpGet]
    public async Task<IActionResult> LapPhieuMuon()
    {
        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(LapPhieuMuon),
            TieuDe = "SP_LapPhieuMuon — Lập phiếu mượn sách",
            BaiToan = "Xây dựng thủ tục lập phiếu mượn: kiểm tra thẻ độc giả (khóa/hết hạn/còn nợ) và tình trạng cuốn sách, " +
                      "nếu hợp lệ thì sinh mã phiếu mới, thêm vào PHIEUMUON + CHITIET_PM và đổi trạng thái cuốn sách sang 'Đã cho mượn' (trong 1 transaction).",
            CauSql = "EXEC SP_LapPhieuMuon @MaDocGia, @MaCuonSach, @HanPhaiTra",
            Truong =
            {
                new TruongNhap { Ten = "MaDocGia", Nhan = "Độc giả", Loai = "select", Options = await DdDocGiaAsync(null) },
                new TruongNhap { Ten = "MaCuonSach", Nhan = "Cuốn sách (còn trong kho)", Loai = "select", Options = await DdCuonSachAsync("Còn trong kho", null) },
                new TruongNhap { Ten = "HanPhaiTra", Nhan = "Hạn phải trả", Loai = "date" },
            },
            BangTruoc = await ChupAsync(BangLapPhieu),
        };
        return View("Demo", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LapPhieuMuon(string MaDocGia, string MaCuonSach, DateTime HanPhaiTra)
    {
        var truoc = await ChupAsync(BangLapPhieu);
        var kq = await _svc.ChayProcAsync("SP_LapPhieuMuon",
            ("MaDocGia", MaDocGia), ("MaCuonSach", MaCuonSach), ("HanPhaiTra", HanPhaiTra));
        var sau = await ChupAsync(BangLapPhieu);

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(LapPhieuMuon),
            TieuDe = "SP_LapPhieuMuon — Lập phiếu mượn sách",
            BaiToan = "Lập phiếu mượn (kiểm tra điều kiện độc giả + cuốn sách, sinh mã phiếu, cập nhật 3 bảng trong transaction).",
            CauSql = ExecSql("SP_LapPhieuMuon", ("MaDocGia", MaDocGia), ("MaCuonSach", MaCuonSach), ("HanPhaiTra", HanPhaiTra)),
            Truong =
            {
                new TruongNhap { Ten = "MaDocGia", Nhan = "Độc giả", Loai = "select", Options = await DdDocGiaAsync(MaDocGia) },
                new TruongNhap { Ten = "MaCuonSach", Nhan = "Cuốn sách (còn trong kho)", Loai = "select", Options = await DdCuonSachAsync("Còn trong kho", MaCuonSach) },
                new TruongNhap { Ten = "HanPhaiTra", Nhan = "Hạn phải trả", Loai = "date" },
            },
            BangTruoc = truoc,
            BangSau = sau,
            Output = kq.ThongDiep,
            ThanhCong = kq.ThanhCong,
            GiaTri = { ["HanPhaiTra"] = HanPhaiTra.ToString("yyyy-MM-dd") },
        };
        return View("Demo", vm);
    }

    // ===================== 2) SP_TraSach =====================
    private static readonly (string, string)[] BangTraSach =
    {
        ("CHITIET_PM (mới nhất)", "SELECT TOP 50 MaPhieuMuon, MaCuonSach, NgayTraThucTe, HienTrangKhiTra, TienPhat FROM CHITIET_PM ORDER BY MaPhieuMuon DESC"),
        ("CUONSACH", "SELECT TOP 50 MaCuonSach, MaDauSach, TrangThaiKho, HienTrangSach FROM CUONSACH ORDER BY MaCuonSach"),
    };

    [HttpGet]
    public async Task<IActionResult> TraSach()
    {
        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(TraSach),
            TieuDe = "SP_TraSach — Trả sách",
            BaiToan = "Xây dựng thủ tục trả sách: ghi nhận ngày trả thực tế + hiện trạng khi trả vào CHITIET_PM và " +
                      "cập nhật cuốn sách về 'Còn trong kho' (kiểm tra phiếu/cuốn tồn tại và chưa trả trước đó).",
            CauSql = "EXEC SP_TraSach @MaPhieuMuon, @MaCuonSach, @HienTrangKhiTra",
            Truong =
            {
                new TruongNhap { Ten = "MaPhieuMuon", Nhan = "Phiếu mượn", Loai = "select", Options = await DdPhieuMuonAsync(null) },
                new TruongNhap { Ten = "MaCuonSach", Nhan = "Cuốn sách (đang cho mượn)", Loai = "select", Options = await DdCuonSachAsync("Đã cho mượn", null) },
                new TruongNhap { Ten = "HienTrangKhiTra", Nhan = "Hiện trạng khi trả", Loai = "select", Options = DdHienTrang(null) },
            },
            BangTruoc = await ChupAsync(BangTraSach),
        };
        return View("Demo", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TraSach(string MaPhieuMuon, string MaCuonSach, string HienTrangKhiTra)
    {
        var truoc = await ChupAsync(BangTraSach);
        var kq = await _svc.ChayProcAsync("SP_TraSach",
            ("MaPhieuMuon", MaPhieuMuon), ("MaCuonSach", MaCuonSach), ("HienTrangKhiTra", HienTrangKhiTra));
        var sau = await ChupAsync(BangTraSach);

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(TraSach),
            TieuDe = "SP_TraSach — Trả sách",
            BaiToan = "Trả sách: cập nhật CHITIET_PM (ngày trả, hiện trạng) và CUONSACH (về 'Còn trong kho').",
            CauSql = ExecSql("SP_TraSach", ("MaPhieuMuon", MaPhieuMuon), ("MaCuonSach", MaCuonSach), ("HienTrangKhiTra", HienTrangKhiTra)),
            Truong =
            {
                new TruongNhap { Ten = "MaPhieuMuon", Nhan = "Phiếu mượn", Loai = "select", Options = await DdPhieuMuonAsync(MaPhieuMuon) },
                new TruongNhap { Ten = "MaCuonSach", Nhan = "Cuốn sách", Loai = "select", Options = await DdCuonSachAsync(null, MaCuonSach) },
                new TruongNhap { Ten = "HienTrangKhiTra", Nhan = "Hiện trạng khi trả", Loai = "select", Options = DdHienTrang(HienTrangKhiTra) },
            },
            BangTruoc = truoc,
            BangSau = sau,
            Output = kq.ThongDiep,
            ThanhCong = kq.ThanhCong,
        };
        return View("Demo", vm);
    }

    // ===================== 3) SP_ThuTienPhat =====================
    private static readonly (string, string)[] BangDocGia =
    {
        ("DOCGIA", "SELECT MaDocGia, HoTen, NgayHetHan, TongNo, TrangThai FROM DOCGIA ORDER BY MaDocGia"),
    };

    [HttpGet]
    public async Task<IActionResult> ThuTienPhat()
    {
        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(ThuTienPhat),
            TieuDe = "SP_ThuTienPhat — Thu tiền phạt / trả nợ",
            BaiToan = "Xây dựng thủ tục thu tiền phạt của độc giả: trừ vào TongNo, nếu hết nợ thì tự mở khóa thẻ " +
                      "(kiểm tra độc giả tồn tại, số tiền > 0 và không vượt quá nợ hiện tại).",
            CauSql = "EXEC SP_ThuTienPhat @MaDocGia, @SoTienThu",
            Truong =
            {
                new TruongNhap { Ten = "MaDocGia", Nhan = "Độc giả", Loai = "select", Options = await DdDocGiaAsync(null) },
                new TruongNhap { Ten = "SoTienThu", Nhan = "Số tiền thu (VNĐ)", Loai = "number" },
            },
            BangTruoc = await ChupAsync(BangDocGia),
        };
        return View("Demo", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThuTienPhat(string MaDocGia, decimal SoTienThu)
    {
        var truoc = await ChupAsync(BangDocGia);
        var kq = await _svc.ChayProcAsync("SP_ThuTienPhat", ("MaDocGia", MaDocGia), ("SoTienThu", SoTienThu));
        var sau = await ChupAsync(BangDocGia);

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(ThuTienPhat),
            TieuDe = "SP_ThuTienPhat — Thu tiền phạt / trả nợ",
            BaiToan = "Thu tiền phạt: trừ TongNo, hết nợ thì mở khóa thẻ.",
            CauSql = ExecSql("SP_ThuTienPhat", ("MaDocGia", MaDocGia), ("SoTienThu", SoTienThu)),
            Truong =
            {
                new TruongNhap { Ten = "MaDocGia", Nhan = "Độc giả", Loai = "select", Options = await DdDocGiaAsync(MaDocGia) },
                new TruongNhap { Ten = "SoTienThu", Nhan = "Số tiền thu (VNĐ)", Loai = "number" },
            },
            BangTruoc = truoc,
            BangSau = sau,
            Output = kq.ThongDiep,
            ThanhCong = kq.ThanhCong,
            GiaTri = { ["SoTienThu"] = SoTienThu.ToString(CultureInfo.InvariantCulture) },
        };
        return View("Demo", vm);
    }

    // ===================== 4) SP_NhapSachKho =====================
    private static readonly (string, string)[] BangNhapKho =
    {
        ("CUONSACH (mới nhất)", "SELECT TOP 50 MaCuonSach, MaDauSach, TrangThaiKho, HienTrangSach FROM CUONSACH ORDER BY MaCuonSach DESC"),
        ("DAUSACH", "SELECT MaDauSach, TenSach, TacGia, TheLoai FROM DAUSACH ORDER BY MaDauSach"),
    };

    [HttpGet]
    public async Task<IActionResult> NhapSachKho()
    {
        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(NhapSachKho),
            TieuDe = "SP_NhapSachKho — Nhập cuốn sách mới vào kho",
            BaiToan = "Xây dựng thủ tục nhập 1 cuốn sách mới vào kho thuộc một đầu sách có sẵn " +
                      "(kiểm tra đầu sách tồn tại, mã cuốn chưa trùng), mặc định 'Còn trong kho'.",
            CauSql = "EXEC SP_NhapSachKho @MaCuonSach, @MaDauSach, @HienTrang",
            Truong =
            {
                new TruongNhap { Ten = "MaCuonSach", Nhan = "Mã cuốn sách (tự đặt, vd CS01_05)", Loai = "text" },
                new TruongNhap { Ten = "MaDauSach", Nhan = "Đầu sách", Loai = "select", Options = await DdDauSachAsync(null) },
                new TruongNhap { Ten = "HienTrang", Nhan = "Hiện trạng", Loai = "select", Options = DdHienTrang("Mới") },
            },
            BangTruoc = await ChupAsync(BangNhapKho),
        };
        return View("Demo", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NhapSachKho(string MaCuonSach, string MaDauSach, string HienTrang)
    {
        var truoc = await ChupAsync(BangNhapKho);
        var kq = await _svc.ChayProcAsync("SP_NhapSachKho",
            ("MaCuonSach", MaCuonSach), ("MaDauSach", MaDauSach), ("HienTrang", HienTrang));
        var sau = await ChupAsync(BangNhapKho);

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(NhapSachKho),
            TieuDe = "SP_NhapSachKho — Nhập cuốn sách mới vào kho",
            BaiToan = "Nhập cuốn sách mới vào kho (kiểm tra đầu sách tồn tại + mã cuốn chưa trùng).",
            CauSql = ExecSql("SP_NhapSachKho", ("MaCuonSach", MaCuonSach), ("MaDauSach", MaDauSach), ("HienTrang", HienTrang)),
            Truong =
            {
                new TruongNhap { Ten = "MaCuonSach", Nhan = "Mã cuốn sách (tự đặt, vd CS01_05)", Loai = "text" },
                new TruongNhap { Ten = "MaDauSach", Nhan = "Đầu sách", Loai = "select", Options = await DdDauSachAsync(MaDauSach) },
                new TruongNhap { Ten = "HienTrang", Nhan = "Hiện trạng", Loai = "select", Options = DdHienTrang(HienTrang) },
            },
            BangTruoc = truoc,
            BangSau = sau,
            Output = kq.ThongDiep,
            ThanhCong = kq.ThanhCong,
            GiaTri = { ["MaCuonSach"] = MaCuonSach },
        };
        return View("Demo", vm);
    }

    // ===================== 5) SP_GiaHanThe =====================
    [HttpGet]
    public async Task<IActionResult> GiaHanThe()
    {
        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(GiaHanThe),
            TieuDe = "SP_GiaHanThe — Gia hạn thẻ độc giả",
            BaiToan = "Xây dựng thủ tục gia hạn thẻ: cộng thêm số tháng vào NgayHetHan của độc giả " +
                      "(kiểm tra độc giả tồn tại, số tháng > 0).",
            CauSql = "EXEC SP_GiaHanThe @MaDocGia, @SoThangGiaHan",
            Truong =
            {
                new TruongNhap { Ten = "MaDocGia", Nhan = "Độc giả", Loai = "select", Options = await DdDocGiaAsync(null) },
                new TruongNhap { Ten = "SoThangGiaHan", Nhan = "Số tháng gia hạn", Loai = "number" },
            },
            BangTruoc = await ChupAsync(BangDocGia),
        };
        return View("Demo", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GiaHanThe(string MaDocGia, int SoThangGiaHan)
    {
        var truoc = await ChupAsync(BangDocGia);
        var kq = await _svc.ChayProcAsync("SP_GiaHanThe", ("MaDocGia", MaDocGia), ("SoThangGiaHan", SoThangGiaHan));
        var sau = await ChupAsync(BangDocGia);

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(GiaHanThe),
            TieuDe = "SP_GiaHanThe — Gia hạn thẻ độc giả",
            BaiToan = "Gia hạn thẻ: cộng số tháng vào NgayHetHan.",
            CauSql = ExecSql("SP_GiaHanThe", ("MaDocGia", MaDocGia), ("SoThangGiaHan", SoThangGiaHan)),
            Truong =
            {
                new TruongNhap { Ten = "MaDocGia", Nhan = "Độc giả", Loai = "select", Options = await DdDocGiaAsync(MaDocGia) },
                new TruongNhap { Ten = "SoThangGiaHan", Nhan = "Số tháng gia hạn", Loai = "number" },
            },
            BangTruoc = truoc,
            BangSau = sau,
            Output = kq.ThongDiep,
            ThanhCong = kq.ThanhCong,
            GiaTri = { ["SoThangGiaHan"] = SoThangGiaHan.ToString(CultureInfo.InvariantCulture) },
        };
        return View("Demo", vm);
    }

    // ===================== Helpers =====================
    private async Task<List<BangKetQua>> ChupAsync((string TieuDe, string Sql)[] queries)
    {
        var list = new List<BangKetQua>();
        foreach (var (tieuDe, sql) in queries)
            list.Add(await _svc.TruyVanAsync(tieuDe, sql));
        return list;
    }

    private async Task<SelectList> DdDocGiaAsync(string? sel)
    {
        var items = await _db.Docgia.AsNoTracking().OrderBy(d => d.MaDocGia)
            .Select(d => new { d.MaDocGia, Hien = d.MaDocGia + " - " + d.HoTen + " (nợ: " + d.TongNo + ")" })
            .ToListAsync();
        return new SelectList(items, "MaDocGia", "Hien", sel);
    }

    private async Task<SelectList> DdCuonSachAsync(string? trangThaiKho, string? sel)
    {
        var q = _db.Cuonsaches.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(trangThaiKho)) q = q.Where(c => c.TrangThaiKho == trangThaiKho);
        var items = await q.OrderBy(c => c.MaCuonSach)
            .Select(c => new { c.MaCuonSach, Hien = c.MaCuonSach + " (" + c.TrangThaiKho + ")" })
            .ToListAsync();
        return new SelectList(items, "MaCuonSach", "Hien", sel);
    }

    private async Task<SelectList> DdPhieuMuonAsync(string? sel)
    {
        var items = await _db.Phieumuons.AsNoTracking().OrderByDescending(p => p.MaPhieuMuon)
            .Select(p => new { p.MaPhieuMuon, Hien = p.MaPhieuMuon + " - " + p.MaDocGia })
            .ToListAsync();
        return new SelectList(items, "MaPhieuMuon", "Hien", sel);
    }

    private async Task<SelectList> DdDauSachAsync(string? sel)
    {
        var items = await _db.Dausaches.AsNoTracking().OrderBy(d => d.MaDauSach)
            .Select(d => new { d.MaDauSach, Hien = d.MaDauSach + " - " + d.TenSach })
            .ToListAsync();
        return new SelectList(items, "MaDauSach", "Hien", sel);
    }

    private static SelectList DdHienTrang(string? sel)
    {
        var ds = new[] { "Bình thường", "Mới", "Cũ", "Rách", "Hư", "Rách nát", "Hỏng nặng", "Mất" };
        return new SelectList(ds, sel);
    }

    private static string ExecSql(string proc, params (string Ten, object? GiaTri)[] ps)
        => $"EXEC {proc} " + string.Join(", ", ps.Select(p => $"@{p.Ten} = {Fmt(p.GiaTri)}"));

    private static string Fmt(object? v) => v switch
    {
        null => "NULL",
        string s => $"N'{s}'",
        DateTime d => $"'{d:yyyy-MM-dd}'",
        DateOnly d => $"'{d:yyyy-MM-dd}'",
        _ => Convert.ToString(v, CultureInfo.InvariantCulture) ?? "NULL",
    };
}
