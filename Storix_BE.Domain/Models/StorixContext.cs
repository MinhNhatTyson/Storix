using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Storix_BE.Domain.Models;

public partial class StorixContext : DbContext
{
    public StorixContext()
    {
    }

    public StorixContext(DbContextOptions<StorixContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<StorageZone> StorageZones { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=Storix;Username=postgres;Password=12345;Include Error Detail=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("products_pkey");

            entity.ToTable("products");

            entity.HasIndex(e => e.Sku, "products_sku_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Height).HasColumnName("height");
            entity.Property(e => e.Image).HasColumnName("image");
            entity.Property(e => e.IsCold).HasColumnName("isCold");
            entity.Property(e => e.IsEsd).HasColumnName("isEsd");
            entity.Property(e => e.IsHighValue).HasColumnName("isHighValue");
            entity.Property(e => e.IsMsd).HasColumnName("isMsd");
            entity.Property(e => e.IsVulnerable).HasColumnName("isVulnerable");
            entity.Property(e => e.Length).HasColumnName("length");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.PopularityScore).HasColumnName("popularity_score");
            entity.Property(e => e.Sku)
                .HasColumnType("character varying")
                .HasColumnName("sku");
            entity.Property(e => e.Unit)
                .HasColumnType("character varying")
                .HasColumnName("unit");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.Weight).HasColumnName("weight");
            entity.Property(e => e.Width).HasColumnName("width");
        });

        modelBuilder.Entity<StorageZone>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("storage_zones_pkey");

            entity.ToTable("storage_zones");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Height).HasColumnName("height");
            entity.Property(e => e.IdCode).HasColumnName("id_code");
            entity.Property(e => e.Image)
                .HasColumnType("character varying")
                .HasColumnName("image");
            entity.Property(e => e.IsCold).HasColumnName("isCold");
            entity.Property(e => e.IsEsd).HasColumnName("isESD");
            entity.Property(e => e.IsHighValue).HasColumnName("isHighValue");
            entity.Property(e => e.IsMsd).HasColumnName("isMSD");
            entity.Property(e => e.IsVulnerable).HasColumnName("isVulnerable");
            entity.Property(e => e.Length).HasColumnName("length");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
            entity.Property(e => e.Width).HasColumnName("width");
            entity.Property(e => e.XCoordinate).HasColumnName("x_coordinate");
            entity.Property(e => e.YCoordinate).HasColumnName("y_coordinate");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
