namespace Nhom11.Services;

/// <summary>
/// Cấu hình nghiệp vụ thư viện, đọc từ section "ThuVien" trong appsettings.json.
/// </summary>
public class ThuVienOptions
{
    /// <summary>Tiền phạt cho mỗi ngày trả trễ (VND).</summary>
    public decimal PhatQuaHanMoiNgay { get; set; } = 5000;

    /// <summary>Tổng nợ vượt ngưỡng này thì thẻ độc giả tự động bị khóa.</summary>
    public decimal NguongKhoaThe { get; set; } = 50000;

    /// <summary>Số cuốn sách tối đa được mượn trong một phiếu.</summary>
    public int SoSachMuonToiDa { get; set; } = 5;

    /// <summary>Số ngày mượn mặc định khi lập phiếu (dùng để gợi ý hạn trả).</summary>
    public int SoNgayMuonMacDinh { get; set; } = 14;
}
