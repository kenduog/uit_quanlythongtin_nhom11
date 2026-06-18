using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nhom11.Models.ViewModels;

/// <summary>Kết quả 1 câu truy vấn bảng (để render dạng bảng generic trong demo).</summary>
public class BangKetQua
{
    public string TieuDe { get; set; } = string.Empty;
    public string Sql { get; set; } = string.Empty;
    public string[] Cot { get; set; } = Array.Empty<string>();
    public List<string?[]> Dong { get; set; } = new();
    public int SoDongNoiBat { get; set; }   // số dòng đầu tiên là dòng mới/thay đổi (để highlight)
}

/// <summary>Một ô nhập tham số trên form demo (render generic theo Loai).</summary>
public class TruongNhap
{
    public string Ten { get; set; } = string.Empty;   // tên field = tên tham số proc
    public string Nhan { get; set; } = string.Empty;   // nhãn hiển thị
    public string Loai { get; set; } = "text";          // text | number | date | select
    public SelectList? Options { get; set; }            // cho loai = select
    public string? GhiChu { get; set; }
    public bool TaiLaiKhiDoi { get; set; }              // true: đổi select sẽ reload trang (GET) để nạp lại dropdown phụ thuộc
}

/// <summary>Dữ liệu cho 1 trang demo stored procedure theo bố cục B1–B5.</summary>
public class ThuTucDemoViewModel
{
    public string ActionName { get; set; } = string.Empty;
    public string TieuDe { get; set; } = string.Empty;        // tiêu đề trang
    public string BaiToan { get; set; } = string.Empty;        // B1
    public string CauSql { get; set; } = string.Empty;         // B2 (mẫu EXEC, hoặc EXEC thật sau khi chạy)
    public List<TruongNhap> Truong { get; set; } = new();      // B4 các ô nhập
    public List<BangKetQua> BangTruoc { get; set; } = new();   // B3
    public List<BangKetQua>? BangSau { get; set; }             // B5 (null khi chưa chạy)
    public string? Output { get; set; }                        // B5 output proc
    public bool? ThanhCong { get; set; }                       // null=chưa chạy
    public Dictionary<string, string?> GiaTri { get; set; } = new(); // giá trị đã nhập (repopulate)
}
