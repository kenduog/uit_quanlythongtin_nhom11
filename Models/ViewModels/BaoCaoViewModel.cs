namespace Nhom11.Models.ViewModels;

/// <summary>Tổng hợp dữ liệu cho trang báo cáo - thống kê.</summary>
public class BaoCaoViewModel
{
    public DateOnly TuNgay { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));
    public DateOnly DenNgay { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public List<SachDangMuonItem> SachDangMuon { get; set; } = new();
    public List<PhieuQuaHanItem> PhieuQuaHan { get; set; } = new();
    public List<DocGiaNoItem> DocGiaCoNo { get; set; } = new();
    public List<SachMuonNhieuItem> SachMuonNhieu { get; set; } = new();
    public decimal TongTienPhatTrongKy { get; set; }
}

public class SachDangMuonItem
{
    public string MaCuonSach { get; set; } = string.Empty;
    public string TenSach { get; set; } = string.Empty;
    public string HoTenDocGia { get; set; } = string.Empty;
    public string MaPhieuMuon { get; set; } = string.Empty;
    public DateOnly HanPhaiTra { get; set; }
}

public class DocGiaNoItem
{
    public string MaDocGia { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public decimal TongNo { get; set; }
    public string? TrangThai { get; set; }
}

public class SachMuonNhieuItem
{
    public string TenSach { get; set; } = string.Empty;
    public string TacGia { get; set; } = string.Empty;
    public int SoLuotMuon { get; set; }
}
