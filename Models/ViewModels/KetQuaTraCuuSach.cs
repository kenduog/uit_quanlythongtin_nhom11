namespace Nhom11.Models.ViewModels;

/// <summary>Một dòng kết quả tra cứu sách (mức cuốn sách).</summary>
public class KetQuaTraCuuSach
{
    public string MaCuonSach { get; set; } = string.Empty;
    public string TenSach { get; set; } = string.Empty;
    public string TacGia { get; set; } = string.Empty;
    public string TheLoai { get; set; } = string.Empty;
    public string? TrangThaiKho { get; set; }
    public string? HienTrangSach { get; set; }
}
