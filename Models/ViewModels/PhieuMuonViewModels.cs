using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nhom11.Models.Entities;

namespace Nhom11.Models.ViewModels;

/// <summary>Dữ liệu cho màn hình lập phiếu mượn.</summary>
public class LapPhieuMuonViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn độc giả.")]
    [Display(Name = "Độc giả")]
    public string MaDocGia { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn ngày hạn trả.")]
    [DataType(DataType.Date)]
    [Display(Name = "Hạn phải trả")]
    public DateOnly HanPhaiTra { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(14));

    [Display(Name = "Cuốn sách mượn")]
    public List<string> DsMaCuonSach { get; set; } = new();

    // Nguồn dữ liệu cho dropdown / danh sách chọn.
    public SelectList? DocGiaList { get; set; }
    public List<CuonSachConTrongKho> CuonSachConTrongKho { get; set; } = new();
}

/// <summary>Một cuốn sách còn trong kho để chọn khi lập phiếu.</summary>
public record CuonSachConTrongKho(string MaCuonSach, string TenSach, string TacGia, string? HienTrangSach);

/// <summary>Dữ liệu cho màn hình trả sách của một phiếu.</summary>
public class TraSachViewModel
{
    public Phieumuon Phieu { get; set; } = null!;
    public List<DongTraSachViewModel> CacDong { get; set; } = new();
}

public class DongTraSachViewModel
{
    public string MaCuonSach { get; set; } = string.Empty;
    public string TenSach { get; set; } = string.Empty;
    public bool DaTra { get; set; }
    public DateOnly? NgayTraThucTe { get; set; }
    public decimal? TienPhatHienTai { get; set; }

    // Người dùng nhập khi trả.
    public bool ChonTra { get; set; }
    public string? HienTrangKhiTra { get; set; }
}
