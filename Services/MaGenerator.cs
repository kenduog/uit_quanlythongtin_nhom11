using Microsoft.EntityFrameworkCore;
using Nhom11.Data;

namespace Nhom11.Services;

/// <summary>
/// Sinh mã định danh tự tăng theo tiền tố cho các bảng trong hệ thống.
/// </summary>
public class MaGenerator
{
    private readonly DoAnNhom11Context _db;

    public MaGenerator(DoAnNhom11Context db)
    {
        _db = db;
    }

    /// <summary>Sinh mã đầu sách kế tiếp dạng DSxxx (DS001, DS002, ...).</summary>
    public async Task<string> SinhMaDauSachAsync()
    {
        var maxSo = await _db.Dausaches
            .Where(d => d.MaDauSach.StartsWith("DS"))
            .Select(d => d.MaDauSach.Substring(2))
            .ToListAsync();
        int next = LaySoLonNhat(maxSo) + 1;
        return $"DS{next:D3}";
    }

    /// <summary>Sinh mã độc giả kế tiếp dạng DGxxx (DG001, DG002, ...).</summary>
    public async Task<string> SinhMaDocGiaAsync()
    {
        var dsSo = await _db.Docgia
            .Where(d => d.MaDocGia.StartsWith("DG"))
            .Select(d => d.MaDocGia.Substring(2))
            .ToListAsync();
        int next = LaySoLonNhat(dsSo) + 1;
        return $"DG{next:D3}";
    }

    /// <summary>Sinh mã phiếu mượn kế tiếp dạng PMxxx (PM001, PM002, ...).</summary>
    public async Task<string> SinhMaPhieuMuonAsync()
    {
        var dsSo = await _db.Phieumuons
            .Where(p => p.MaPhieuMuon.StartsWith("PM"))
            .Select(p => p.MaPhieuMuon.Substring(2))
            .ToListAsync();
        int next = LaySoLonNhat(dsSo) + 1;
        return $"PM{next:D3}";
    }

    /// <summary>
    /// Sinh mã cuốn sách kế tiếp cho một đầu sách dạng CSxx_yy,
    /// trong đó xx lấy từ phần số của mã đầu sách, yy tự tăng.
    /// </summary>
    public async Task<string> SinhMaCuonSachAsync(string maDauSach)
    {
        // Format: {MaDauSach}_CS{thứTự} → DS01_CS01, DS01_CS02, ...
        string tienTo = $"{maDauSach}_CS";
        var dsSo = await _db.Cuonsaches
            .Where(c => c.MaCuonSach.StartsWith(tienTo))
            .Select(c => c.MaCuonSach.Substring(tienTo.Length))
            .ToListAsync();
        int next = LaySoLonNhat(dsSo) + 1;
        return $"{tienTo}{next:D2}";
    }

    private static int LaySoLonNhat(IEnumerable<string> phanSo)
    {
        int max = 0;
        foreach (var s in phanSo)
        {
            if (int.TryParse(s, out int n) && n > max) max = n;
        }
        return max;
    }
}
