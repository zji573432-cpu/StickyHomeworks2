using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using StickyHomeworks.Core.Entities;

namespace StickyHomeworks.Core.Context;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Emotion> Emotions { get; set; }

    public virtual DbSet<EmotionsGroup> EmotionsGroups { get; set; }

    public virtual DbSet<Homework> Homeworks { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=db/app.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Emotion>(entity =>
        {
            entity.ToTable("emotions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.Path).HasColumnName("path");
            entity.Property(e => e.Size).HasColumnName("size");
        });

        modelBuilder.Entity<EmotionsGroup>(entity =>
        {
            entity.ToTable("emotions_groups");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Size)
                .HasColumnType("NUMBER")
                .HasColumnName("size");
        });

        modelBuilder.Entity<Homework>(entity =>
        {
            entity.ToTable("homework");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.EndTime)
                .HasColumnType("DATETIME")
                .HasColumnName("end_time");
            entity.Property(e => e.Subject).HasColumnName("subject");
            entity.Property(e => e.Tags).HasColumnName("tags");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
