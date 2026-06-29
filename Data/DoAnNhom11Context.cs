using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Nhom11.Models.Entities;

namespace Nhom11.Data;

public partial class DoAnNhom11Context : DbContext
{
    public DoAnNhom11Context(DbContextOptions<DoAnNhom11Context> options)
        : base(options)
    {
    }

    public virtual DbSet<ChitietPm> ChitietPms { get; set; }

    public virtual DbSet<Cuonsach> Cuonsaches { get; set; }

    public virtual DbSet<Dausach> Dausaches { get; set; }

    public virtual DbSet<Docgium> Docgia { get; set; }

    public virtual DbSet<Phieumuon> Phieumuons { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChitietPm>(entity =>
        {
            entity.HasKey(e => new { e.MaCuonSach, e.MaPhieuMuon }).HasName("PK__CHITIET___9C42EA4FAFE9767B");

            entity.ToTable("CHITIET_PM");

            entity.Property(e => e.MaCuonSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaPhieuMuon)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.HienTrangKhiTra).HasMaxLength(50);
            entity.Property(e => e.TienPhat)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 0)");

            entity.HasOne(d => d.MaCuonSachNavigation).WithMany(p => p.ChitietPms)
                .HasForeignKey(d => d.MaCuonSach)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CHITIET_P__MaCuo__4AB81AF0");

            entity.HasOne(d => d.MaPhieuMuonNavigation).WithMany(p => p.ChitietPms)
                .HasForeignKey(d => d.MaPhieuMuon)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CHITIET_P__MaPhi__4BAC3F29");
        });

        modelBuilder.Entity<Cuonsach>(entity =>
        {
            entity.HasKey(e => e.MaCuonSach).HasName("PK__CUONSACH__A00E686D23176431");

            entity.ToTable("CUONSACH");

            entity.Property(e => e.MaCuonSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.HienTrangSach)
                .HasMaxLength(50)
                .HasDefaultValue("Bình thường");
            entity.Property(e => e.MaDauSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TrangThaiKho)
                .HasMaxLength(50)
                .HasDefaultValue("Còn trong kho");

            entity.HasOne(d => d.MaDauSachNavigation).WithMany(p => p.Cuonsaches)
                .HasForeignKey(d => d.MaDauSach)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CUONSACH__MaDauS__3B75D760");
        });

        modelBuilder.Entity<Dausach>(entity =>
        {
            entity.HasKey(e => e.MaDauSach).HasName("PK__DAUSACH__AB6F2B5F598669DB");

            entity.ToTable("DAUSACH");

            entity.Property(e => e.MaDauSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TacGia).HasMaxLength(150);
            entity.Property(e => e.TenSach).HasMaxLength(255);
            entity.Property(e => e.TheLoai).HasMaxLength(100);
        });

        modelBuilder.Entity<Docgium>(entity =>
        {
            entity.HasKey(e => e.MaDocGia).HasName("PK__DOCGIA__F165F945BC9A74D9");

            entity.ToTable("DOCGIA");

            entity.Property(e => e.MaDocGia)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.HoTen).HasMaxLength(150);
            entity.Property(e => e.NgayLapThe).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TongNo)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 0)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Bình thường");
        });

        modelBuilder.Entity<Phieumuon>(entity =>
        {
            entity.HasKey(e => e.MaPhieuMuon).HasName("PK__PHIEUMUO__C4C82222F89F65A1");

            entity.ToTable("PHIEUMUON");

            entity.Property(e => e.MaPhieuMuon)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaDocGia)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayMuon).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.MaDocGiaNavigation).WithMany(p => p.Phieumuons)
                .HasForeignKey(d => d.MaDocGia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PHIEUMUON__MaDoc__46E78A0C");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
