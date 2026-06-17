namespace Nhom11.Services;

/// <summary>Kết quả trả về của một thao tác nghiệp vụ (thành công/thất bại + thông điệp).</summary>
public class KetQua
{
    public bool ThanhCong { get; init; }
    public string ThongDiep { get; init; } = string.Empty;
    public string? MaThamChieu { get; init; }

    public static KetQua Ok(string thongDiep, string? maThamChieu = null)
        => new() { ThanhCong = true, ThongDiep = thongDiep, MaThamChieu = maThamChieu };

    public static KetQua Loi(string thongDiep)
        => new() { ThanhCong = false, ThongDiep = thongDiep };
}
