﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ScrapLinkedInDall.Database.Models;

namespace ScrapLinkedInDall.Database.Contexto;

public partial class LinkedInContext : DbContext
{
    public LinkedInContext()
    {
    }

    public LinkedInContext(DbContextOptions<LinkedInContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Education> Educations { get; set; }

    public virtual DbSet<Experience> Experiences { get; set; }

    public virtual DbSet<LicenseCertification> LicenseCertifications { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=NAKAMA;Initial Catalog=LinkedIn;Integrated Security=True;Encrypt=False");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Education>(entity =>
        {
            entity.HasKey(e => e.EducationId).HasName("PK__Educatio__4BBE3805C67A366F");

            entity.HasOne(d => d.UserProfile).WithMany(p => p.Educations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Education__UserP__3C69FB99");
        });

        modelBuilder.Entity<Experience>(entity =>
        {
            entity.HasKey(e => e.ExperienceId).HasName("PK__Experien__2F4E3449EDA17196");

            entity.HasOne(d => d.UserProfile).WithMany(p => p.Experiences)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Experienc__UserP__398D8EEE");
        });

        modelBuilder.Entity<LicenseCertification>(entity =>
        {
            entity.HasKey(e => e.LicenseCertificationId).HasName("PK__LicenseC__CE7A16DCA958A5CB");

            entity.HasOne(d => d.UserProfile).WithMany(p => p.LicenseCertifications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LicenseCe__UserP__3F466844");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.UserProfileId).HasName("PK__UserProf__9E267F62FC1B2A0A");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}