using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Gis_Api.Models;

public partial class GISSHP2Context : DbContext
{
    public GISSHP2Context(DbContextOptions<GISSHP2Context> options)
        : base(options)
    {
    }

    public virtual DbSet<FileUpload> FileUpload { get; set; }

    public virtual DbSet<Spot> Spot { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileUpload>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_FileUp");

            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.Geo)
                .IsRequired()
                .HasColumnType("geometry");
            entity.Property(e => e.GeoJson).IsRequired();
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Wkt).IsRequired();
        });

        modelBuilder.Entity<Spot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_spot");

            entity.Property(e => e.Id)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Address).HasMaxLength(150);
            entity.Property(e => e.County).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Tel)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Town).HasMaxLength(100);
            entity.Property(e => e.geom)
                .IsRequired()
                .HasColumnType("geometry");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
