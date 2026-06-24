using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nhom11.Data;
using Nhom11.Models.ViewModels;
using Nhom11.Services;

namespace Nhom11.Controllers;

/// <summary>
/// Demo phần TỰ ĐỘNG HÓA (Thăng): 3 Function + 5 Trigger + 2 Cursor.
/// Dùng lại bố cục B1–B5 của demo Thủ tục (view Shared/Demo.cshtml + ThuTucDemoViewModel).
/// Mọi thao tác đều gọi ĐÚNG đối tượng SQL trong DB (SELECT hàm / chạy DML kích hoạt trigger / EXEC proc-cursor),
/// KHÔNG viết lại logic bằng C#.
/// </summary>
public class TuDongHoaController : Controller
{
    private readonly DoAnNhom11Context _db;
    private readonly IThuTucService _svc;
    private readonly MaGenerator _maGen;

    public TuDongHoaController(DoAnNhom11Context db, IThuTucService svc, MaGenerator maGen)
    {
        _db = db;
        _svc = svc;
        _maGen = maGen;
    }

    public IActionResult Index() => View();

    // ============================================================================
    //  FUNCTIONS — gọi qua TruyVanAsync (SELECT dbo.FUNC_...)
    // ============================================================================

    // 1) FUNC_TinhTienTre(@HanPhaiTra, @NgayTraThucTe) → số ngày trễ × 5.000đ
    [HttpGet]
    public IActionResult TinhTienTre()
    {
        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(TinhTienTre),
            TieuDe = "FUNC_TinhTienTre — Tính tiền phạt trễ hạn",
            BaiToan = "Hàm vô hướng nhận hạn phải trả + ngày trả thực tế, trả về tiền phạt trễ = số ngày trễ × 5.000đ " +
                      "(trả về 0 nếu trả đúng hạn hoặc sớm). Web chỉ gọi SELECT dbo.FUNC_TinhTienTre(...), không tự tính.",
            CauSql = "SELECT dbo.FUNC_TinhTienTre(@HanPhaiTra, @NgayTraThucTe) AS TienPhatTre",
            Truong =
            {
                new TruongNhap { Ten = "HanPhaiTra", Nhan = "Hạn phải trả", Loai = "date" },
                new TruongNhap { Ten = "NgayTraThucTe", Nhan = "Ngày trả thực tế", Loai = "date" },
            },
            GiaTri =
            {
                ["HanPhaiTra"] = DateTime.Today.AddDays(-10).ToString("yyyy-MM-dd"),
                ["NgayTraThucTe"] = DateTime.Today.ToString("yyyy-MM-dd"),
            },
        };
        return View("Demo", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TinhTienTre(DateTime HanPhaiTra, DateTime NgayTraThucTe)
    {
        const string sql = "SELECT @HanPhaiTra AS HanPhaiTra, @NgayTraThucTe AS NgayTraThucTe, " +
                           "DATEDIFF(DAY, @HanPhaiTra, @NgayTraThucTe) AS SoNgayTre, " +
                           "dbo.FUNC_TinhTienTre(@HanPhaiTra, @NgayTraThucTe) AS [TienPhatTre (đ)]";
        var ketQua = await _svc.TruyVanAsync("Kết quả FUNC_TinhTienTre", sql,
            ("HanPhaiTra", HanPhaiTra), ("NgayTraThucTe", NgayTraThucTe));

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(TinhTienTre),
            TieuDe = "FUNC_TinhTienTre — Tính tiền phạt trễ hạn",
            BaiToan = "Gọi hàm tính tiền phạt trễ hạn (số ngày trễ × 5.000đ).",
            CauSql = $"SELECT dbo.FUNC_TinhTienTre('{HanPhaiTra:yyyy-MM-dd}', '{NgayTraThucTe:yyyy-MM-dd}') AS TienPhatTre",
            Truong =
            {
                new TruongNhap { Ten = "HanPhaiTra", Nhan = "Hạn phải trả", Loai = "date" },
                new TruongNhap { Ten = "NgayTraThucTe", Nhan = "Ngày trả thực tế", Loai = "date" },
            },
            BangSau = new List<BangKetQua> { ketQua },
            Output = "Đã gọi hàm dbo.FUNC_TinhTienTre thành công — xem kết quả ở bảng bên dưới.",
            ThanhCong = true,
            GiaTri =
            {
                ["HanPhaiTra"] = HanPhaiTra.ToString("yyyy-MM-dd"),
                ["NgayTraThucTe"] = NgayTraThucTe.ToString("yyyy-MM-dd"),
            },
        };
        return View("Demo", vm);
    }

    // 2) FUNC_KiemTraThe(@MaDocGia) → 1 nếu thẻ hợp lệ, 0 nếu khóa/hết hạn/nợ>50k
    [HttpGet]
    public async Task<IActionResult> KiemTraThe()
    {
        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(KiemTraThe),
            TieuDe = "FUNC_KiemTraThe — Kiểm tra thẻ độc giả còn hợp lệ",
            BaiToan = "Hàm trả về BIT: 1 nếu thẻ Bình thường + chưa hết hạn + nợ ≤ 50.000đ; ngược lại trả 0. " +
                      "Chọn một độc giả rồi gọi SELECT dbo.FUNC_KiemTraThe(@MaDocGia).",
            CauSql = "SELECT dbo.FUNC_KiemTraThe(@MaDocGia) AS TheHopLe",
            Truong =
            {
                new TruongNhap { Ten = "MaDocGia", Nhan = "Độc giả", Loai = "select", Options = await DdDocGiaChiTietAsync(null) },
            },
            BangTruoc = await ChupAsync(BangDocGia),
        };
        return View("Demo", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KiemTraThe(string MaDocGia)
    {
        const string sql = "SELECT d.MaDocGia, d.HoTen, d.TrangThai, d.NgayHetHan, d.TongNo, " +
                           "dbo.FUNC_KiemTraThe(@MaDocGia) AS TheHopLe " +
                           "FROM DOCGIA d WHERE d.MaDocGia = @MaDocGia";
        var ketQua = await _svc.TruyVanAsync("Kết quả FUNC_KiemTraThe (1 = hợp lệ, 0 = không)", sql,
            ("MaDocGia", MaDocGia));

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(KiemTraThe),
            TieuDe = "FUNC_KiemTraThe — Kiểm tra thẻ độc giả còn hợp lệ",
            BaiToan = "Gọi hàm kiểm tra thẻ độc giả còn hợp lệ hay không (1/0).",
            CauSql = $"SELECT dbo.FUNC_KiemTraThe(N'{MaDocGia}') AS TheHopLe",
            Truong =
            {
                new TruongNhap { Ten = "MaDocGia", Nhan = "Độc giả", Loai = "select", Options = await DdDocGiaChiTietAsync(MaDocGia) },
            },
            BangTruoc = await ChupAsync(BangDocGia),
            BangSau = new List<BangKetQua> { ketQua },
            Output = "Đã gọi hàm dbo.FUNC_KiemTraThe — cột TheHopLe: 1 = thẻ hợp lệ, 0 = bị khóa/hết hạn/nợ > 50.000đ.",
            ThanhCong = true,
        };
        return View("Demo", vm);
    }

    // 3) FUNC_ThongKeNo(@MaDocGia) → tổng nợ hiện tại
    [HttpGet]
    public async Task<IActionResult> ThongKeNo()
    {
        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(ThongKeNo),
            TieuDe = "FUNC_ThongKeNo — Thống kê nợ của độc giả",
            BaiToan = "Hàm trả về tổng nợ hiện tại (TongNo) của một độc giả. Gọi SELECT dbo.FUNC_ThongKeNo(@MaDocGia).",
            CauSql = "SELECT dbo.FUNC_ThongKeNo(@MaDocGia) AS TongNo",
            Truong =
            {
                new TruongNhap { Ten = "MaDocGia", Nhan = "Độc giả", Loai = "select", Options = await DdDocGiaChiTietAsync(null) },
            },
            BangTruoc = await ChupAsync(BangDocGia),
        };
        return View("Demo", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThongKeNo(string MaDocGia)
    {
        const string sql = "SELECT d.MaDocGia, d.HoTen, dbo.FUNC_ThongKeNo(@MaDocGia) AS [TongNo (đ)] " +
                           "FROM DOCGIA d WHERE d.MaDocGia = @MaDocGia";
        var ketQua = await _svc.TruyVanAsync("Kết quả FUNC_ThongKeNo", sql, ("MaDocGia", MaDocGia));

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(ThongKeNo),
            TieuDe = "FUNC_ThongKeNo — Thống kê nợ của độc giả",
            BaiToan = "Gọi hàm thống kê tổng nợ hiện tại của độc giả.",
            CauSql = $"SELECT dbo.FUNC_ThongKeNo(N'{MaDocGia}') AS TongNo",
            Truong =
            {
                new TruongNhap { Ten = "MaDocGia", Nhan = "Độc giả", Loai = "select", Options = await DdDocGiaChiTietAsync(MaDocGia) },
            },
            BangTruoc = await ChupAsync(BangDocGia),
            BangSau = new List<BangKetQua> { ketQua },
            Output = "Đã gọi hàm dbo.FUNC_ThongKeNo thành công.",
            ThanhCong = true,
        };
        return View("Demo", vm);
    }

    // ============================================================================
    //  TRIGGERS — gọi qua ChayLenhAsync (INSERT/UPDATE thật để kích hoạt trigger)
    // ============================================================================

    // 1) TRG_ChanMuonSach — INSERT PHIEUMUON; chặn nếu thẻ khóa/hết hạn/nợ>50k
    [HttpGet]
    public async Task<IActionResult> ChanMuonSach()
    {
        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(ChanMuonSach),
            TieuDe = "TRG_ChanMuonSach — Chặn lập phiếu mượn cho thẻ không hợp lệ",
            BaiToan = "Trigger AFTER INSERT trên PHIEUMUON: nếu độc giả hết hạn thẻ / nợ > 50.000đ / bị khóa thì RAISERROR + ROLLBACK. " +
                      "Hãy chọn một độc giả BỊ KHÓA hoặc NỢ CAO để thấy trigger chặn (báo lỗi đỏ), hoặc độc giả bình thường để thấy phiếu mới được thêm.",
            CauSql = "INSERT INTO PHIEUMUON(MaPhieuMuon, MaDocGia, NgayMuon, HanPhaiTra) VALUES (@MaPhieuMuon, @MaDocGia, @NgayMuon, @HanPhaiTra)",
            Truong =
            {
                new TruongNhap { Ten = "MaDocGia", Nhan = "Độc giả (xem trạng thái/nợ để chọn)", Loai = "select", Options = await DdDocGiaChiTietAsync(null) },
                new TruongNhap { Ten = "HanPhaiTra", Nhan = "Hạn phải trả", Loai = "date" },
            },
            GiaTri = { ["HanPhaiTra"] = DateTime.Today.AddDays(14).ToString("yyyy-MM-dd") },
            BangTruoc = await ChupAsync(BangPhieuDocGia),
        };
        return View("Demo", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChanMuonSach(string MaDocGia, DateTime HanPhaiTra)
    {
        var maPhieu = await _maGen.SinhMaPhieuMuonAsync();
        var truoc = await ChupAsync(BangPhieuDocGia);
        var kq = await _svc.ChayLenhAsync(
            "INSERT INTO PHIEUMUON(MaPhieuMuon, MaDocGia, NgayMuon, HanPhaiTra) VALUES (@MaPhieuMuon, @MaDocGia, @NgayMuon, @HanPhaiTra)",
            ("MaPhieuMuon", maPhieu), ("MaDocGia", MaDocGia), ("NgayMuon", DateTime.Today), ("HanPhaiTra", HanPhaiTra));
        var sau = await ChupAsync(BangPhieuDocGia);
        DanhDauThayDoi(truoc, sau);

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(ChanMuonSach),
            TieuDe = "TRG_ChanMuonSach — Chặn lập phiếu mượn cho thẻ không hợp lệ",
            BaiToan = "INSERT phiếu mượn mới — TRG_ChanMuonSach sẽ ROLLBACK nếu thẻ hết hạn / nợ > 50.000đ / bị khóa.",
            CauSql = $"INSERT INTO PHIEUMUON(MaPhieuMuon, MaDocGia, NgayMuon, HanPhaiTra)\nVALUES (N'{maPhieu}', N'{MaDocGia}', '{DateTime.Today:yyyy-MM-dd}', '{HanPhaiTra:yyyy-MM-dd}')",
            Truong =
            {
                new TruongNhap { Ten = "MaDocGia", Nhan = "Độc giả (xem trạng thái/nợ để chọn)", Loai = "select", Options = await DdDocGiaChiTietAsync(MaDocGia) },
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

    // 2) TRG_XuatKho — INSERT CHITIET_PM → cuốn sách tự đổi 'Đã cho mượn'
    [HttpGet]
    public async Task<IActionResult> XuatKho()
    {
        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(XuatKho),
            TieuDe = "TRG_XuatKho — Tự xuất kho khi thêm chi tiết mượn",
            BaiToan = "Trigger AFTER INSERT trên CHITIET_PM: tự cập nhật cuốn sách vừa mượn sang trạng thái 'Đã cho mượn'. " +
                      "Chọn 1 phiếu mượn và 1 cuốn sách CÒN TRONG KHO rồi thực thi để thấy trạng thái kho đổi tự động.",
            CauSql = "INSERT INTO CHITIET_PM(MaPhieuMuon, MaCuonSach, TienPhat) VALUES (@MaPhieuMuon, @MaCuonSach, 0)",
            Truong =
            {
                new TruongNhap { Ten = "MaPhieuMuon", Nhan = "Phiếu mượn", Loai = "select", Options = await DdPhieuMuonAsync(null) },
                new TruongNhap { Ten = "MaCuonSach", Nhan = "Cuốn sách (còn trong kho)", Loai = "select", Options = await DdCuonSachAsync("Còn trong kho", null) },
            },
            BangTruoc = await ChupAsync(BangChiTietCuon),
        };
        return View("Demo", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XuatKho(string MaPhieuMuon, string MaCuonSach)
    {
        var truoc = await ChupAsync(BangChiTietCuon);
        var kq = await _svc.ChayLenhAsync(
            "INSERT INTO CHITIET_PM(MaPhieuMuon, MaCuonSach, TienPhat) VALUES (@MaPhieuMuon, @MaCuonSach, 0)",
            ("MaPhieuMuon", MaPhieuMuon), ("MaCuonSach", MaCuonSach));
        var sau = await ChupAsync(BangChiTietCuon);
        DanhDauThayDoi(truoc, sau);

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(XuatKho),
            TieuDe = "TRG_XuatKho — Tự xuất kho khi thêm chi tiết mượn",
            BaiToan = "INSERT chi tiết mượn — TRG_XuatKho tự đổi cuốn sách sang 'Đã cho mượn'.",
            CauSql = $"INSERT INTO CHITIET_PM(MaPhieuMuon, MaCuonSach, TienPhat)\nVALUES (N'{MaPhieuMuon}', N'{MaCuonSach}', 0)",
            Truong =
            {
                new TruongNhap { Ten = "MaPhieuMuon", Nhan = "Phiếu mượn", Loai = "select", Options = await DdPhieuMuonAsync(MaPhieuMuon) },
                new TruongNhap { Ten = "MaCuonSach", Nhan = "Cuốn sách (còn trong kho)", Loai = "select", Options = await DdCuonSachAsync("Còn trong kho", MaCuonSach) },
            },
            BangTruoc = truoc,
            BangSau = sau,
            Output = kq.ThongDiep,
            ThanhCong = kq.ThanhCong,
        };
        return View("Demo", vm);
    }

    // 3) TRG_NhapKho — UPDATE CHITIET_PM (NgayTraThucTe) → cuốn sách về 'Còn trong kho'
    [HttpGet]
    public async Task<IActionResult> NhapKho(string? MaPhieuMuon)
    {
        var phieuChon = MaPhieuMuon ?? await _db.Phieumuons.AsNoTracking()
            .Where(p => p.ChitietPms.Any(ct => ct.NgayTraThucTe == null))
            .OrderByDescending(p => p.MaPhieuMuon)
            .Select(p => p.MaPhieuMuon)
            .FirstOrDefaultAsync();

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(NhapKho),
            TieuDe = "TRG_NhapKho — Tự nhập kho khi ghi nhận ngày trả",
            BaiToan = "Trigger AFTER UPDATE trên CHITIET_PM: khi điền NgayTraThucTe (sách được trả), tự cập nhật cuốn sách về 'Còn trong kho'. " +
                      "Chọn 1 cuốn đang mượn (chưa trả) của một phiếu rồi thực thi.",
            CauSql = "UPDATE CHITIET_PM SET NgayTraThucTe = @NgayTra WHERE MaPhieuMuon = @MaPhieuMuon AND MaCuonSach = @MaCuonSach",
            Truong =
            {
                new TruongNhap { Ten = "MaPhieuMuon", Nhan = "Phiếu mượn (còn sách chưa trả)", Loai = "select", Options = await DdPhieuMuonChuaTraAsync(phieuChon), TaiLaiKhiDoi = true },
                new TruongNhap { Ten = "MaCuonSach", Nhan = "Cuốn sách (chưa trả của phiếu)", Loai = "select", Options = await DdCuonSachCuaPhieuAsync(phieuChon, null) },
            },
            BangTruoc = await ChupAsync(BangChiTietCuon),
        };
        return View("Demo", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NhapKho(string MaPhieuMuon, string MaCuonSach)
    {
        var truoc = await ChupAsync(BangChiTietCuon);
        var kq = await _svc.ChayLenhAsync(
            "UPDATE CHITIET_PM SET NgayTraThucTe = @NgayTra WHERE MaPhieuMuon = @MaPhieuMuon AND MaCuonSach = @MaCuonSach",
            ("NgayTra", DateTime.Today), ("MaPhieuMuon", MaPhieuMuon), ("MaCuonSach", MaCuonSach));
        var sau = await ChupAsync(BangChiTietCuon);
        DanhDauThayDoi(truoc, sau);

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(NhapKho),
            TieuDe = "TRG_NhapKho — Tự nhập kho khi ghi nhận ngày trả",
            BaiToan = "UPDATE NgayTraThucTe — TRG_NhapKho tự đổi cuốn sách về 'Còn trong kho'.",
            CauSql = $"UPDATE CHITIET_PM SET NgayTraThucTe = '{DateTime.Today:yyyy-MM-dd}'\nWHERE MaPhieuMuon = N'{MaPhieuMuon}' AND MaCuonSach = N'{MaCuonSach}'",
            Truong =
            {
                new TruongNhap { Ten = "MaPhieuMuon", Nhan = "Phiếu mượn (còn sách chưa trả)", Loai = "select", Options = await DdPhieuMuonChuaTraAsync(MaPhieuMuon), TaiLaiKhiDoi = true },
                new TruongNhap { Ten = "MaCuonSach", Nhan = "Cuốn sách (chưa trả của phiếu)", Loai = "select", Options = await DdCuonSachCuaPhieuAsync(MaPhieuMuon, MaCuonSach) },
            },
            BangTruoc = truoc,
            BangSau = sau,
            Output = kq.ThongDiep,
            ThanhCong = kq.ThanhCong,
        };
        return View("Demo", vm);
    }

    // 4) TRG_PhatHuHong — UPDATE HienTrangKhiTra ('Rách'/'Mất') → cộng phạt vào TongNo + TienPhat
    [HttpGet]
    public async Task<IActionResult> PhatHuHong(string? MaPhieuMuon)
    {
        var phieuChon = MaPhieuMuon ?? await _db.Phieumuons.AsNoTracking()
            .Where(p => p.ChitietPms.Any(ct => ct.NgayTraThucTe == null))
            .OrderByDescending(p => p.MaPhieuMuon)
            .Select(p => p.MaPhieuMuon)
            .FirstOrDefaultAsync();

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(PhatHuHong),
            TieuDe = "TRG_PhatHuHong — Tự phạt khi sách bị rách/mất",
            BaiToan = "Trigger AFTER UPDATE trên CHITIET_PM: khi hiện trạng khi trả chứa 'Rách' (50.000đ) hoặc 'Mất' (360.000đ) thì " +
                      "tự cộng tiền phạt vào TongNo của độc giả và vào TienPhat của chi tiết. Nếu nợ vượt 50.000đ có thể kéo theo TRG_KhoaThe khóa thẻ.",
            CauSql = "UPDATE CHITIET_PM SET HienTrangKhiTra = @HienTrang WHERE MaPhieuMuon = @MaPhieuMuon AND MaCuonSach = @MaCuonSach",
            Truong =
            {
                new TruongNhap { Ten = "MaPhieuMuon", Nhan = "Phiếu mượn (còn sách chưa trả)", Loai = "select", Options = await DdPhieuMuonChuaTraAsync(phieuChon), TaiLaiKhiDoi = true },
                new TruongNhap { Ten = "MaCuonSach", Nhan = "Cuốn sách (chưa trả của phiếu)", Loai = "select", Options = await DdCuonSachCuaPhieuAsync(phieuChon, null) },
                new TruongNhap { Ten = "HienTrang", Nhan = "Hiện trạng khi trả", Loai = "select", Options = DdHienTrangHuHong(null), GhiChu = "Chứa 'Rách' → 50k, chứa 'Mất' → 360k" },
            },
            BangTruoc = await ChupAsync(BangDocGiaChiTiet),
        };
        return View("Demo", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PhatHuHong(string MaPhieuMuon, string MaCuonSach, string HienTrang)
    {
        var truoc = await ChupAsync(BangDocGiaChiTiet);
        var kq = await _svc.ChayLenhAsync(
            "UPDATE CHITIET_PM SET HienTrangKhiTra = @HienTrang WHERE MaPhieuMuon = @MaPhieuMuon AND MaCuonSach = @MaCuonSach",
            ("HienTrang", HienTrang), ("MaPhieuMuon", MaPhieuMuon), ("MaCuonSach", MaCuonSach));
        var sau = await ChupAsync(BangDocGiaChiTiet);
        DanhDauThayDoi(truoc, sau);

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(PhatHuHong),
            TieuDe = "TRG_PhatHuHong — Tự phạt khi sách bị rách/mất",
            BaiToan = "UPDATE hiện trạng khi trả — TRG_PhatHuHong cộng tiền phạt vào TongNo + TienPhat (có thể cascade TRG_KhoaThe).",
            CauSql = $"UPDATE CHITIET_PM SET HienTrangKhiTra = N'{HienTrang}'\nWHERE MaPhieuMuon = N'{MaPhieuMuon}' AND MaCuonSach = N'{MaCuonSach}'",
            Truong =
            {
                new TruongNhap { Ten = "MaPhieuMuon", Nhan = "Phiếu mượn (còn sách chưa trả)", Loai = "select", Options = await DdPhieuMuonChuaTraAsync(MaPhieuMuon), TaiLaiKhiDoi = true },
                new TruongNhap { Ten = "MaCuonSach", Nhan = "Cuốn sách (chưa trả của phiếu)", Loai = "select", Options = await DdCuonSachCuaPhieuAsync(MaPhieuMuon, MaCuonSach) },
                new TruongNhap { Ten = "HienTrang", Nhan = "Hiện trạng khi trả", Loai = "select", Options = DdHienTrangHuHong(HienTrang), GhiChu = "Chứa 'Rách' → 50k, chứa 'Mất' → 360k" },
            },
            BangTruoc = truoc,
            BangSau = sau,
            Output = kq.ThongDiep,
            ThanhCong = kq.ThanhCong,
        };
        return View("Demo", vm);
    }

    // 5) TRG_KhoaThe — UPDATE DOCGIA.TongNo > 50000 → tự khóa thẻ
    [HttpGet]
    public async Task<IActionResult> KhoaThe()
    {
        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(KhoaThe),
            TieuDe = "TRG_KhoaThe — Tự khóa thẻ khi nợ vượt 50.000đ",
            BaiToan = "Trigger AFTER UPDATE trên DOCGIA: khi TongNo > 50.000đ thì tự đổi TrangThai sang 'Bị khóa'. " +
                      "Chọn 1 độc giả và nhập số nợ mới > 50.000đ để thấy thẻ bị khóa tự động.",
            CauSql = "UPDATE DOCGIA SET TongNo = @TongNo WHERE MaDocGia = @MaDocGia",
            Truong =
            {
                new TruongNhap { Ten = "MaDocGia", Nhan = "Độc giả", Loai = "select", Options = await DdDocGiaChiTietAsync(null) },
                new TruongNhap { Ten = "TongNo", Nhan = "Số nợ mới (VNĐ)", Loai = "number", GhiChu = "Nhập > 50.000 để kích hoạt khóa thẻ" },
            },
            GiaTri = { ["TongNo"] = "60000" },
            BangTruoc = await ChupAsync(BangDocGia),
        };
        return View("Demo", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KhoaThe(string MaDocGia, decimal TongNo)
    {
        var truoc = await ChupAsync(BangDocGia);
        var kq = await _svc.ChayLenhAsync(
            "UPDATE DOCGIA SET TongNo = @TongNo WHERE MaDocGia = @MaDocGia",
            ("TongNo", TongNo), ("MaDocGia", MaDocGia));
        var sau = await ChupAsync(BangDocGia);
        DanhDauThayDoi(truoc, sau);

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(KhoaThe),
            TieuDe = "TRG_KhoaThe — Tự khóa thẻ khi nợ vượt 50.000đ",
            BaiToan = "UPDATE TongNo — TRG_KhoaThe tự đổi TrangThai sang 'Bị khóa' khi nợ > 50.000đ.",
            CauSql = $"UPDATE DOCGIA SET TongNo = {TongNo.ToString(CultureInfo.InvariantCulture)} WHERE MaDocGia = N'{MaDocGia}'",
            Truong =
            {
                new TruongNhap { Ten = "MaDocGia", Nhan = "Độc giả", Loai = "select", Options = await DdDocGiaChiTietAsync(MaDocGia) },
                new TruongNhap { Ten = "TongNo", Nhan = "Số nợ mới (VNĐ)", Loai = "number", GhiChu = "Nhập > 50.000 để kích hoạt khóa thẻ" },
            },
            BangTruoc = truoc,
            BangSau = sau,
            Output = kq.ThongDiep,
            ThanhCong = kq.ThanhCong,
            GiaTri = { ["TongNo"] = TongNo.ToString(CultureInfo.InvariantCulture) },
        };
        return View("Demo", vm);
    }

    // ============================================================================
    //  CURSORS — gọi qua ChayProcLayBangAsync (EXEC SP_CUR_...)
    // ============================================================================

    // 1) SP_CUR_TopSachHot — Top 5 đầu sách mượn nhiều nhất (chỉ đọc)
    [HttpGet]
    public IActionResult TopSachHot()
    {
        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(TopSachHot),
            TieuDe = "SP_CUR_TopSachHot — Top 5 sách được mượn nhiều nhất",
            BaiToan = "Stored procedure dùng CURSOR duyệt qua thống kê số lượt mượn từng đầu sách, in ra (PRINT) và trả về bảng Top 5. " +
                      "Đây là demo chỉ đọc — không thay đổi dữ liệu. Nhấn Thực thi để chạy con trỏ.",
            CauSql = "EXEC dbo.SP_CUR_TopSachHot",
        };
        return View("Demo", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(TopSachHot))]
    public async Task<IActionResult> TopSachHotPost()
    {
        var (bang, kq) = await _svc.ChayProcLayBangAsync("SP_CUR_TopSachHot");

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(TopSachHot),
            TieuDe = "SP_CUR_TopSachHot — Top 5 sách được mượn nhiều nhất",
            BaiToan = "Chạy con trỏ thống kê Top 5 đầu sách mượn nhiều nhất (chỉ đọc).",
            CauSql = "EXEC dbo.SP_CUR_TopSachHot",
            BangSau = bang,
            Output = kq.ThongDiep,
            ThanhCong = kq.ThanhCong,
        };
        return View("Demo", vm);
    }

    // 2) SP_CUR_QuetPhatNguoi — Quét phiếu quá hạn, cộng phí trễ vào TongNo (GHI DỮ LIỆU)
    [HttpGet]
    public async Task<IActionResult> QuetPhatNguoi()
    {
        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(QuetPhatNguoi),
            TieuDe = "SP_CUR_QuetPhatNguoi — Quét & cộng phí trễ hạn",
            BaiToan = "Stored procedure dùng CURSOR duyệt mọi cuốn đang mượn đã quá hạn, tính phí trễ (số ngày × 5.000đ) và CỘNG vào TongNo của độc giả. " +
                      "⚠ Demo này GHI dữ liệu thật và cộng dồn mỗi lần chạy — chỉ nên chạy 1 lần để minh hoạ.",
            CauSql = "EXEC dbo.SP_CUR_QuetPhatNguoi",
            BangTruoc = await ChupAsync(BangDocGiaQuaHan),
        };
        return View("Demo", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(QuetPhatNguoi))]
    public async Task<IActionResult> QuetPhatNguoiPost()
    {
        var truoc = await ChupAsync(BangDocGiaQuaHan);
        var (_, kq) = await _svc.ChayProcLayBangAsync("SP_CUR_QuetPhatNguoi");
        var sau = await ChupAsync(BangDocGiaQuaHan);
        DanhDauThayDoi(truoc, sau);

        var vm = new ThuTucDemoViewModel
        {
            ActionName = nameof(QuetPhatNguoi),
            TieuDe = "SP_CUR_QuetPhatNguoi — Quét & cộng phí trễ hạn",
            BaiToan = "Chạy con trỏ quét phiếu quá hạn và cộng phí trễ vào TongNo của độc giả (ghi dữ liệu thật).",
            CauSql = "EXEC dbo.SP_CUR_QuetPhatNguoi",
            BangTruoc = truoc,
            BangSau = sau,
            Output = kq.ThongDiep,
            ThanhCong = kq.ThanhCong,
        };
        return View("Demo", vm);
    }

    // ============================================================================
    //  Bộ truy vấn chụp bảng TRƯỚC/SAU
    // ============================================================================
    private static readonly (string, string)[] BangDocGia =
    {
        ("DOCGIA", "SELECT MaDocGia, HoTen, NgayHetHan, TongNo, TrangThai FROM DOCGIA ORDER BY MaDocGia"),
    };

    private static readonly (string, string)[] BangPhieuDocGia =
    {
        ("PHIEUMUON (mới nhất)", "SELECT TOP 50 MaPhieuMuon, MaDocGia, NgayMuon, HanPhaiTra FROM PHIEUMUON ORDER BY MaPhieuMuon DESC"),
        ("DOCGIA", "SELECT MaDocGia, HoTen, NgayHetHan, TongNo, TrangThai FROM DOCGIA ORDER BY MaDocGia"),
    };

    private static readonly (string, string)[] BangChiTietCuon =
    {
        ("CHITIET_PM (mới nhất)", "SELECT TOP 50 MaPhieuMuon, MaCuonSach, NgayTraThucTe, HienTrangKhiTra, TienPhat FROM CHITIET_PM ORDER BY MaPhieuMuon DESC"),
        ("CUONSACH", "SELECT TOP 50 MaCuonSach, MaDauSach, TrangThaiKho, HienTrangSach FROM CUONSACH ORDER BY MaCuonSach"),
    };

    private static readonly (string, string)[] BangDocGiaChiTiet =
    {
        ("DOCGIA", "SELECT MaDocGia, HoTen, NgayHetHan, TongNo, TrangThai FROM DOCGIA ORDER BY MaDocGia"),
        ("CHITIET_PM (mới nhất)", "SELECT TOP 50 MaPhieuMuon, MaCuonSach, NgayTraThucTe, HienTrangKhiTra, TienPhat FROM CHITIET_PM ORDER BY MaPhieuMuon DESC"),
    };

    private static readonly (string, string)[] BangDocGiaQuaHan =
    {
        ("DOCGIA", "SELECT MaDocGia, HoTen, NgayHetHan, TongNo, TrangThai FROM DOCGIA ORDER BY MaDocGia"),
        ("Phiếu QUÁ HẠN chưa trả", @"SELECT pm.MaDocGia, pm.MaPhieuMuon, ct.MaCuonSach, pm.HanPhaiTra,
            DATEDIFF(DAY, pm.HanPhaiTra, CAST(GETDATE() AS DATE)) AS SoNgayTre
            FROM PHIEUMUON pm
            INNER JOIN CHITIET_PM ct ON ct.MaPhieuMuon = pm.MaPhieuMuon
            WHERE ct.NgayTraThucTe IS NULL AND pm.HanPhaiTra < CAST(GETDATE() AS DATE)
            ORDER BY pm.MaDocGia, pm.MaPhieuMuon"),
    };

    // ============================================================================
    //  Helpers (chụp bảng, diff, dropdown) — cùng khuôn với ThuTucController
    // ============================================================================
    private async Task<List<BangKetQua>> ChupAsync((string TieuDe, string Sql)[] queries)
    {
        var list = new List<BangKetQua>();
        foreach (var (tieuDe, sql) in queries)
            list.Add(await _svc.TruyVanAsync(tieuDe, sql));
        return list;
    }

    /// <summary>So sánh diff TRƯỚC/SAU: dòng mới/đổi đưa lên đầu bảng + đánh dấu.</summary>
    private static void DanhDauThayDoi(List<BangKetQua> truoc, List<BangKetQua> sau)
    {
        var mapCu = truoc.ToDictionary(b => b.TieuDe, b => b.Dong.Select(KhoaDong).ToHashSet());
        foreach (var b in sau)
        {
            if (!mapCu.TryGetValue(b.TieuDe, out var cu)) continue;
            var moi = b.Dong.Where(d => !cu.Contains(KhoaDong(d))).ToList();
            var giu = b.Dong.Where(d => cu.Contains(KhoaDong(d))).ToList();
            b.Dong = moi.Concat(giu).ToList();
            b.SoDongNoiBat = moi.Count;
        }
    }

    private static string KhoaDong(string?[] d) => string.Join("", d.Select(x => x ?? " "));

    private async Task<SelectList> DdDocGiaChiTietAsync(string? sel)
    {
        var items = await _db.Docgia.AsNoTracking().OrderBy(d => d.MaDocGia)
            .Select(d => new { d.MaDocGia, Hien = d.MaDocGia + " - " + d.HoTen + " | " + d.TrangThai + " | nợ " + d.TongNo + " | HH " + d.NgayHetHan })
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

    private async Task<SelectList> DdPhieuMuonChuaTraAsync(string? sel)
    {
        var items = await _db.Phieumuons.AsNoTracking()
            .Where(p => p.ChitietPms.Any(ct => ct.NgayTraThucTe == null))
            .OrderByDescending(p => p.MaPhieuMuon)
            .Select(p => new { p.MaPhieuMuon, Hien = p.MaPhieuMuon + " - " + p.MaDocGia })
            .ToListAsync();
        return new SelectList(items, "MaPhieuMuon", "Hien", sel);
    }

    private async Task<SelectList> DdCuonSachCuaPhieuAsync(string? maPhieuMuon, string? sel)
    {
        var items = await _db.ChitietPms.AsNoTracking()
            .Where(ct => ct.MaPhieuMuon == maPhieuMuon && ct.NgayTraThucTe == null)
            .OrderBy(ct => ct.MaCuonSach)
            .Select(ct => new { ct.MaCuonSach, Hien = ct.MaCuonSach + " - " + ct.MaCuonSachNavigation.MaDauSachNavigation.TenSach })
            .ToListAsync();
        return new SelectList(items, "MaCuonSach", "Hien", sel);
    }

    private static SelectList DdHienTrangHuHong(string? sel)
    {
        var ds = new[] { "Bình thường", "Rách bìa nhẹ", "Rách nát", "Hỏng nặng", "Mất" };
        return new SelectList(ds, sel);
    }
}
