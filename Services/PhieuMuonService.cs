using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nhom11.Data;
using Nhom11.Models.Entities;

namespace Nhom11.Services;

public class PhieuMuonService : IPhieuMuonService
{
    public const string TrangThaiConTrongKho = "Còn trong kho";
    public const string TrangThaiDaChoMuon = "Đã cho mượn";
    public const string DocGiaBinhThuong = "Bình thường";
    public const string DocGiaBiKhoa = "Bị khóa";

    private readonly DoAnNhom11Context _db;
    private readonly MaGenerator _maGen;
    private readonly ThuVienOptions _opt;

    public PhieuMuonService(DoAnNhom11Context db, MaGenerator maGen, IOptions<ThuVienOptions> opt)
    {
        _db = db;
        _maGen = maGen;
        _opt = opt.Value;
    }

    public async Task<KetQua> LapPhieuMuonAsync(string maDocGia, IReadOnlyCollection<string> dsMaCuonSach, DateOnly hanPhaiTra)
    {
        var homNay = DateOnly.FromDateTime(DateTime.Today);

        if (string.IsNullOrWhiteSpace(maDocGia))
            return KetQua.Loi("Vui lòng chọn độc giả.");

        var dsCuon = dsMaCuonSach.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().ToList();
        if (dsCuon.Count == 0)
            return KetQua.Loi("Vui lòng chọn ít nhất một cuốn sách để mượn.");

        if (dsCuon.Count > _opt.SoSachMuonToiDa)
            return KetQua.Loi($"Mỗi phiếu chỉ được mượn tối đa {_opt.SoSachMuonToiDa} cuốn.");

        if (hanPhaiTra <= homNay)
            return KetQua.Loi("Hạn phải trả phải lớn hơn ngày hôm nay.");

        var docGia = await _db.Docgia.FirstOrDefaultAsync(d => d.MaDocGia == maDocGia);
        if (docGia == null)
            return KetQua.Loi("Không tìm thấy độc giả.");

        if (docGia.TrangThai == DocGiaBiKhoa)
            return KetQua.Loi($"Thẻ độc giả '{docGia.HoTen}' đang bị khóa, không thể mượn sách.");

        if (docGia.NgayHetHan < homNay)
            return KetQua.Loi($"Thẻ độc giả '{docGia.HoTen}' đã hết hạn ({docGia.NgayHetHan:dd/MM/yyyy}).");

        if ((docGia.TongNo ?? 0) > _opt.NguongKhoaThe)
            return KetQua.Loi($"Độc giả đang nợ {docGia.TongNo:N0}đ, vượt ngưỡng cho phép ({_opt.NguongKhoaThe:N0}đ).");

        // Lấy các cuốn sách và kiểm tra tình trạng kho.
        var cuonSachs = await _db.Cuonsaches
            .Where(c => dsCuon.Contains(c.MaCuonSach))
            .ToListAsync();

        if (cuonSachs.Count != dsCuon.Count)
            return KetQua.Loi("Một số cuốn sách được chọn không tồn tại.");

        var khongSan = cuonSachs.Where(c => c.TrangThaiKho != TrangThaiConTrongKho).ToList();
        if (khongSan.Count > 0)
            return KetQua.Loi($"Các cuốn sau không còn trong kho: {string.Join(", ", khongSan.Select(c => c.MaCuonSach))}.");

        await using var tran = await _db.Database.BeginTransactionAsync();
        try
        {
            var maPhieu = await _maGen.SinhMaPhieuMuonAsync();
            var phieu = new Phieumuon
            {
                MaPhieuMuon = maPhieu,
                MaDocGia = maDocGia,
                NgayMuon = homNay,
                HanPhaiTra = hanPhaiTra
            };
            _db.Phieumuons.Add(phieu);

            foreach (var cuon in cuonSachs)
            {
                _db.ChitietPms.Add(new ChitietPm
                {
                    MaPhieuMuon = maPhieu,
                    MaCuonSach = cuon.MaCuonSach,
                    TienPhat = 0
                });
                // TRG_XuatKho tự set TrangThaiKho = 'Đã cho mượn' khi INSERT CHITIET_PM.
            }

            await _db.SaveChangesAsync();
            await tran.CommitAsync();
            return KetQua.Ok($"Đã lập phiếu mượn {maPhieu} cho độc giả {docGia.HoTen}.", maPhieu);
        }
        catch (Exception ex)
        {
            await tran.RollbackAsync();
            return KetQua.Loi($"Lỗi khi lập phiếu mượn: {ex.Message}");
        }
    }

    public async Task<KetQua> TraSachAsync(string maPhieuMuon, IReadOnlyCollection<DongTraSach> dongTra)
    {
        var homNay = DateOnly.FromDateTime(DateTime.Today);

        var canTra = dongTra.Where(d => !string.IsNullOrWhiteSpace(d.MaCuonSach)).ToList();
        if (canTra.Count == 0)
            return KetQua.Loi("Vui lòng chọn ít nhất một cuốn sách để trả.");

        var phieu = await _db.Phieumuons
            .Include(p => p.MaDocGiaNavigation)
            .Include(p => p.ChitietPms)
                .ThenInclude(ct => ct.MaCuonSachNavigation)
            .FirstOrDefaultAsync(p => p.MaPhieuMuon == maPhieuMuon);

        if (phieu == null)
            return KetQua.Loi("Không tìm thấy phiếu mượn.");

        await using var tran = await _db.Database.BeginTransactionAsync();
        try
        {
            decimal tongPhatQuaHan = 0;
            int soCuonDaTra = 0;

            foreach (var dong in canTra)
            {
                var ct = phieu.ChitietPms.FirstOrDefault(c => c.MaCuonSach == dong.MaCuonSach);
                if (ct == null) continue;            // cuốn không thuộc phiếu
                if (ct.NgayTraThucTe != null) continue; // đã trả rồi, bỏ qua

                // Chỉ tính phạt quá hạn — trigger TRG_PhatHuHong sẽ tự cộng phạt hư hỏng.
                int soNgayTre = Math.Max(0, homNay.DayNumber - phieu.HanPhaiTra.DayNumber);
                decimal phatQuaHan = soNgayTre * _opt.PhatQuaHanMoiNgay;

                ct.NgayTraThucTe = homNay;
                ct.HienTrangKhiTra = dong.HienTrangKhiTra;
                ct.TienPhat = phatQuaHan; // Trigger sẽ += phạt hư hỏng
                tongPhatQuaHan += phatQuaHan;
                soCuonDaTra++;

                // TRG_NhapKho tự set TrangThaiKho = 'Còn trong kho' khi NgayTraThucTe thay đổi.
                var cuon = ct.MaCuonSachNavigation;
                if (cuon != null && !string.IsNullOrWhiteSpace(dong.HienTrangKhiTra))
                {
                    cuon.HienTrangSach = dong.HienTrangKhiTra;
                }
            }

            if (soCuonDaTra == 0)
                return KetQua.Loi("Các cuốn đã chọn đều đã được trả trước đó.");

            // Detach độc giả — trigger đã xử lý TongNo cho phạt hư hỏng,
            // EF không được gửi UPDATE ghi đè.
            var docGia = phieu.MaDocGiaNavigation;
            if (docGia != null)
                _db.Entry(docGia).State = EntityState.Unchanged;

            await _db.SaveChangesAsync();

            // Cộng phạt quá hạn vào TongNo bằng raw SQL (không xung đột với trigger).
            // TRG_KhoaThe sẽ tự khóa thẻ nếu TongNo > 50.000đ.
            if (tongPhatQuaHan > 0)
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE DOCGIA SET TongNo = ISNULL(TongNo, 0) + {0} WHERE MaDocGia = {1}",
                    tongPhatQuaHan, phieu.MaDocGia);
            }

            await tran.CommitAsync();

            var msg = $"Đã ghi nhận trả {soCuonDaTra} cuốn cho phiếu {maPhieuMuon}.";
            if (tongPhatQuaHan > 0) msg += $" Phạt quá hạn: {tongPhatQuaHan:N0}đ.";
            return KetQua.Ok(msg, maPhieuMuon);
        }
        catch (Exception ex)
        {
            await tran.RollbackAsync();
            return KetQua.Loi($"Lỗi khi trả sách: {ex.Message}");
        }
    }

    public decimal TinhTienPhat(DateOnly hanPhaiTra, DateOnly ngayTra, string? hienTrangKhiTra)
    {
        decimal phat = 0;

        // Phạt quá hạn.
        int soNgayTre = ngayTra.DayNumber - hanPhaiTra.DayNumber;
        if (soNgayTre > 0)
            phat += soNgayTre * _opt.PhatQuaHanMoiNgay;

        // Phạt hư hỏng / mất sách theo từ khóa trong hiện trạng khi trả.
        phat += PhatHuHong(hienTrangKhiTra);

        return phat;
    }

    private static decimal PhatHuHong(string? hienTrang)
    {
        if (string.IsNullOrWhiteSpace(hienTrang)) return 0;

        if (hienTrang == "Mất sách") return 360000;
        if (hienTrang == "Hư") return 50000;
        return 0;
    }
}
