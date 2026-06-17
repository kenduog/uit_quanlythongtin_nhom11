using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nhom11.Data;
using Nhom11.Models.ViewModels;

namespace Nhom11.Services;

/// <summary>
/// Gọi stored procedure và truy vấn bảng TRỰC TIẾP trên SQL Server (qua connection của DbContext).
/// Không cache/không lưu dữ liệu trong web — mỗi lần gọi đều đọc sống từ DB.
/// </summary>
public interface IThuTucService
{
    Task<BangKetQua> TruyVanAsync(string tieuDe, string sql, params (string Ten, object? GiaTri)[] thamSo);
    Task<KetQua> ChayProcAsync(string tenProc, params (string Ten, object? GiaTri)[] thamSo);
}

public class ThuTucService : IThuTucService
{
    private readonly DoAnNhom11Context _db;

    public ThuTucService(DoAnNhom11Context db) => _db = db;

    /// <summary>Chạy 1 câu SELECT và trả về cột + dòng (đã ép chuỗi để render bảng generic).</summary>
    public async Task<BangKetQua> TruyVanAsync(string tieuDe, string sql, params (string Ten, object? GiaTri)[] thamSo)
    {
        var bang = new BangKetQua { TieuDe = tieuDe, Sql = sql };
        var conn = (SqlConnection)_db.Database.GetDbConnection();
        bool tuMo = conn.State != ConnectionState.Open;
        if (tuMo) await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            ThemThamSo(cmd, thamSo);
            using var reader = await cmd.ExecuteReaderAsync();

            bang.Cot = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
            while (await reader.ReadAsync())
            {
                var dong = new string?[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                    dong[i] = reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i));
                bang.Dong.Add(dong);
            }
        }
        finally
        {
            if (tuMo) await conn.CloseAsync();
        }
        return bang;
    }

    /// <summary>Chạy stored procedure, gom các dòng PRINT qua InfoMessage; dòng bắt đầu "Err" = thất bại.</summary>
    public async Task<KetQua> ChayProcAsync(string tenProc, params (string Ten, object? GiaTri)[] thamSo)
    {
        var conn = (SqlConnection)_db.Database.GetDbConnection();
        var thongDiep = new List<string>();
        void Handler(object s, SqlInfoMessageEventArgs e)
        {
            foreach (SqlError err in e.Errors)
                if (!string.IsNullOrWhiteSpace(err.Message)) thongDiep.Add(err.Message.Trim());
        }

        conn.InfoMessage += Handler;
        bool tuMo = conn.State != ConnectionState.Open;
        if (tuMo) await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = tenProc;
            ThemThamSo(cmd, thamSo);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqlException ex)
        {
            thongDiep.Add("Err: " + ex.Message);
        }
        finally
        {
            conn.InfoMessage -= Handler;
            if (tuMo) await conn.CloseAsync();
        }

        var noiDung = thongDiep.Count > 0 ? string.Join("\n", thongDiep) : "(Proc không trả về thông báo)";
        bool loi = thongDiep.Any(m => m.TrimStart().StartsWith("Err", StringComparison.OrdinalIgnoreCase));
        return loi ? KetQua.Loi(noiDung) : KetQua.Ok(noiDung);
    }

    private static void ThemThamSo(SqlCommand cmd, (string Ten, object? GiaTri)[] thamSo)
    {
        foreach (var (ten, giaTri) in thamSo)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = ten.StartsWith('@') ? ten : "@" + ten;
            p.Value = giaTri ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }
}
