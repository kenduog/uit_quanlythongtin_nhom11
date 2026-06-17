using Nhom11.Models.Entities;

namespace Nhom11.Models.ViewModels;

/// <summary>Số liệu tổng quan cho trang chủ (dashboard).</summary>
public class DashboardViewModel
{
    public int TongDauSach { get; set; }
    public int TongCuonSach { get; set; }
    public int TongDocGia { get; set; }
    public int SachDangMuon { get; set; }
    public int SachQuaHan { get; set; }
    public int DocGiaBiKhoa { get; set; }
    public decimal TongNo { get; set; }

    /// <summary>Các phiếu chưa trả hết, sắp xếp theo hạn trả gần nhất.</summary>
    public List<PhieuQuaHanItem> PhieuCanChuY { get; set; } = new();
}

public class PhieuQuaHanItem
{
    public string MaPhieuMuon { get; set; } = string.Empty;
    public string HoTenDocGia { get; set; } = string.Empty;
    public DateOnly HanPhaiTra { get; set; }
    public int SoCuonChuaTra { get; set; }
    public int SoNgayTre { get; set; }
}
