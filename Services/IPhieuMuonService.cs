namespace Nhom11.Services;

/// <summary>Một dòng trả sách: cuốn sách + hiện trạng khi trả.</summary>
public record DongTraSach(string MaCuonSach, string? HienTrangKhiTra);

public interface IPhieuMuonService
{
    /// <summary>
    /// Lập phiếu mượn mới cho một độc giả với danh sách cuốn sách.
    /// Kiểm tra toàn bộ ràng buộc nghiệp vụ trước khi ghi.
    /// </summary>
    Task<KetQua> LapPhieuMuonAsync(string maDocGia, IReadOnlyCollection<string> dsMaCuonSach, DateOnly hanPhaiTra);

    /// <summary>
    /// Ghi nhận trả sách cho một phiếu mượn: tính tiền phạt, cập nhật trạng thái
    /// cuốn sách và tổng nợ độc giả trong cùng một transaction.
    /// </summary>
    Task<KetQua> TraSachAsync(string maPhieuMuon, IReadOnlyCollection<DongTraSach> dongTra);

    /// <summary>Tính tiền phạt cho một lần trả (quá hạn + hư hỏng), không ghi DB.</summary>
    decimal TinhTienPhat(DateOnly hanPhaiTra, DateOnly ngayTra, string? hienTrangKhiTra);
}
