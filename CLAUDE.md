# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

`Nhom11` is a **library management web app** ("Quản lý thư viện", group/course project) built with **ASP.NET Core MVC on .NET 9.0** (nullable + implicit usings enabled). It uses **EF Core 9 Database-First** against an existing SQL Server database `DoAn_Nhom11` on `KENSHINE\SQLEXPRESS` (Windows auth). The UI is in Vietnamese (Bootstrap 5). There is no authentication layer.

## Commands

Run from the project root (the folder containing `Nhom11.csproj`):

- Build: `dotnet build`
- Run (HTTP profile, http://localhost:5255): `dotnet run`
- Run with HTTPS (https://localhost:7166): `dotnet run --launch-profile https`
- Hot reload: `dotnet watch run`
- Chia sẻ link public để cùng test: chỉ cần **chạy app như bình thường** (`dotnet run` hoặc F5). Ở môi trường Development, [Services/CloudflareTunnelService.cs](Services/CloudflareTunnelService.cs) (IHostedService) tự bật Cloudflare Tunnel và in `LINK CHIA SE: https://<random>.trycloudflare.com` ra console để copy; người nhận dán link là xem được (không đăng nhập, không trang chặn). Cần `winget install Cloudflare.cloudflared` một lần; link đổi mỗi lần chạy; tắt bằng `"Tunnel": { "Enabled": false }` trong appsettings. `Program.cs` bật `UseForwardedHeaders` ở Development để link qua tunnel hoạt động đúng.
- Re-scaffold entities after DB schema changes (overwrites `Models/Entities` + `Data/`):
  `dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Microsoft.EntityFrameworkCore.SqlServer -o Models/Entities --context-dir Data --context DoAnNhom11Context --no-onconfiguring --force`

No test project exists yet. The connection string lives in `appsettings.json` (`ConnectionStrings:DefaultConnection`); EF packages are pinned to `9.0.*` because the project targets net9.0 (do not bump to EF 10).

## Architecture

- `Data/DoAnNhom11Context.cs` + `Models/Entities/*` — EF Core scaffold (do not hand-edit; regenerate). Five tables map to entities: `Dausach` (đầu sách / book title), `Cuonsach` (cuốn sách / physical copy), `Docgium` (độc giả / reader — note the EF singularizer name; DbSet is `Docgia`), `Phieumuon` (phiếu mượn / borrow slip), `ChitietPm` (chi tiết phiếu mượn, composite key `MaCuonSach`+`MaPhieuMuon`). Dates are `DateOnly`.
- `Services/` — business layer, registered in `Program.cs`:
  - `IPhieuMuonService`/`PhieuMuonService` — the heart of the app: `LapPhieuMuonAsync` (borrow) and `TraSachAsync` (return) run inside a transaction, enforce the borrow rules, compute fines, and keep `Cuonsach.TrangThaiKho` and `Docgium.TongNo` in sync. Also holds the canonical status string constants (`TrangThaiConTrongKho`, `TrangThaiDaChoMuon`, `DocGiaBinhThuong`, `DocGiaBiKhoa`).
  - `MaGenerator` — generates next IDs (`DSxx`, `DGxxx`, `PMxxx`, `CSxx_yy`).
  - `ThuVienOptions` — business config bound from the `ThuVien` section of `appsettings.json` (fine/day, debt lock threshold, max books, default loan days).
  - `KetQua` — success/failure result wrapper returned by service operations; controllers surface `ThongDiep` via `TempData["Success"]`/`["Error"]`.
- `Controllers/` — `DauSach`, `CuonSach`, `DocGia` (full CRUD + search/paging), `PhieuMuon` (Index/Details/Create=lập phiếu/TraSach=trả sách), `TraCuu` (lookup), `BaoCao` (reports), `Home` (dashboard).
- `Models/ViewModels/` — `PagedResult<T>` (paging), `LapPhieuMuonViewModel`/`TraSachViewModel` (borrow/return screens), `DashboardViewModel`, `BaoCaoViewModel`, `KetQuaTraCuuSach`.
- `Views/` — Razor + Bootstrap. `_Layout.cshtml` has the Vietnamese nav + global TempData alerts; `_Pager.cshtml` is the shared pagination partial (set `ViewData["RouteValues"]` to preserve filters). `_ViewImports.cshtml` imports the Entities/ViewModels/Services namespaces.

## Business rules (in `PhieuMuonService`)

- Borrow blocked if reader is `Bị khóa`, card expired (`NgayHetHan < today`), debt over `NguongKhoaThe`, over `SoSachMuonToiDa` books, or a chosen copy is not `Còn trong kho`.
- Fine = overdue days × `PhatQuaHanMoiNgay` + damage fee derived from the return condition string (`PhatHuHong` keyword match: "mất" → 360k, "rách nát"/"hỏng nặng" → 150k, "rách"/"hư" → 50k).
- On return, the reader's `TongNo` increases and the card auto-locks if it exceeds `NguongKhoaThe`.

## Conventions

- Domain naming is Vietnamese (entities, properties, routes like `/PhieuMuon/TraSach`). Match it when extending.
- Status values are compared against the string constants in `PhieuMuonService`; reuse them rather than retyping literals.
- The repo is not under git yet.
