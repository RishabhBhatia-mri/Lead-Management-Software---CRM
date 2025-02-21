using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace LeadManagment.Models;

public partial class LeadsManagementContext : DbContext
{
    public LeadsManagementContext()
    {
    }

    public LeadsManagementContext(DbContextOptions<LeadsManagementContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ImportExportLog> ImportExportLogs { get; set; }

    public virtual DbSet<Lead> Leads { get; set; }

    public virtual DbSet<LeadActivityLog> LeadActivityLogs { get; set; }

    public virtual DbSet<LeadFollowUp> LeadFollowUps { get; set; }

    public virtual DbSet<LeadImportLog> LeadImportLogs { get; set; }

    public virtual DbSet<LeadStatusHistory> LeadStatusHistories { get; set; }

    public virtual DbSet<LeadUpdateLog> LeadUpdateLogs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        if (!optionsBuilder.IsConfigured) {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("lmdb");
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<ImportExportLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PRIMARY");

            entity.ToTable("import_export_logs");

            entity.HasIndex(e => e.Uid, "UId");

            entity.Property(e => e.ActionType)
                .HasColumnType("enum('Imported','Exported')")
                .HasColumnName("Action_Type");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("File_Name");
            entity.Property(e => e.LogTimestamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("Log_Timestamp");
            entity.Property(e => e.Uid).HasColumnName("UId");

            entity.HasOne(d => d.UidNavigation).WithMany(p => p.ImportExportLogs)
                .HasForeignKey(d => d.Uid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("import_export_logs_ibfk_1");
        });

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.HasKey(e => e.Lid).HasName("PRIMARY");

            entity.ToTable("leads");

            entity.HasIndex(e => e.CreatedBy, "Created_By");

            entity.HasIndex(e => e.Email, "Email").IsUnique();

            entity.HasIndex(e => e.ManagerAssigned, "Manager_Assigned");

            entity.HasIndex(e => e.SalesRepAssigned, "SalesRep_Assigned");

            entity.Property(e => e.Lid).HasColumnName("LId");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("Assigned_At");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("Created_At");
            entity.Property(e => e.CreatedBy).HasColumnName("Created_By");
            entity.Property(e => e.ManagerAssigned).HasColumnName("Manager_Assigned");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.SalesRepAssigned).HasColumnName("SalesRep_Assigned");
            entity.Property(e => e.Source).HasColumnType("enum('Website','Reference','Ads','Social Media')");
            entity.Property(e => e.Status).HasColumnType("enum('New','Contacted','Follow-up','Converted','Lost')");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("timestamp")
                .HasColumnName("Updated_At");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.LeadCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("leads_ibfk_3");

            entity.HasOne(d => d.ManagerAssignedNavigation).WithMany(p => p.LeadManagerAssignedNavigations)
                .HasForeignKey(d => d.ManagerAssigned)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("leads_ibfk_1");

            entity.HasOne(d => d.SalesRepAssignedNavigation).WithMany(p => p.LeadSalesRepAssignedNavigations)
                .HasForeignKey(d => d.SalesRepAssigned)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("leads_ibfk_2");
        });

        modelBuilder.Entity<LeadActivityLog>(entity =>
        {
            entity.HasKey(e => e.Aid).HasName("PRIMARY");

            entity.ToTable("lead_activity_log");

            entity.HasIndex(e => e.Lid, "LId");

            entity.HasIndex(e => e.Uid, "UId");

            entity.Property(e => e.Aid).HasColumnName("AId");
            entity.Property(e => e.ActivityDate)
                .HasColumnType("timestamp")
                .HasColumnName("Activity_Date");
            entity.Property(e => e.Lid).HasColumnName("LId");
            entity.Property(e => e.Notes).HasColumnType("text");
            entity.Property(e => e.Uid).HasColumnName("UId");

            entity.HasOne(d => d.LidNavigation).WithMany(p => p.LeadActivityLogs)
                .HasForeignKey(d => d.Lid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("lead_activity_log_ibfk_1");

            entity.HasOne(d => d.UidNavigation).WithMany(p => p.LeadActivityLogs)
                .HasForeignKey(d => d.Uid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("lead_activity_log_ibfk_2");
        });

        modelBuilder.Entity<LeadFollowUp>(entity =>
        {
            entity.HasKey(e => e.Fid).HasName("PRIMARY");

            entity.ToTable("lead_follow_ups");

            entity.HasIndex(e => e.Lid, "LId");

            entity.HasIndex(e => e.Uid, "UId");

            entity.Property(e => e.Fid).HasColumnName("FId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("Created_At");
            entity.Property(e => e.FollowUpDate)
                .HasColumnType("timestamp")
                .HasColumnName("Follow_Up_Date");
            entity.Property(e => e.Lid).HasColumnName("LId");
            entity.Property(e => e.Notes).HasColumnType("text");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pending'")
                .HasColumnType("enum('Pending','Completed','Missed')");
            entity.Property(e => e.Uid).HasColumnName("UId");

            entity.HasOne(d => d.LidNavigation).WithMany(p => p.LeadFollowUps)
                .HasForeignKey(d => d.Lid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("lead_follow_ups_ibfk_1");

            entity.HasOne(d => d.UidNavigation).WithMany(p => p.LeadFollowUps)
                .HasForeignKey(d => d.Uid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("lead_follow_ups_ibfk_2");
        });

        modelBuilder.Entity<LeadImportLog>(entity =>
        {
            entity.HasKey(e => e.ImportId).HasName("PRIMARY");

            entity.ToTable("lead_import_log");

            entity.HasIndex(e => e.ImportedBy, "Imported_By");

            entity.HasIndex(e => e.Lid, "LId");

            entity.HasIndex(e => e.LogId, "LogId");

            entity.Property(e => e.ImportedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("Imported_At");
            entity.Property(e => e.ImportedBy).HasColumnName("Imported_By");
            entity.Property(e => e.Lid).HasColumnName("LId");

            entity.HasOne(d => d.ImportedByNavigation).WithMany(p => p.LeadImportLogs)
                .HasForeignKey(d => d.ImportedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("lead_import_log_ibfk_3");

            entity.HasOne(d => d.LidNavigation).WithMany(p => p.LeadImportLogs)
                .HasForeignKey(d => d.Lid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("lead_import_log_ibfk_2");

            entity.HasOne(d => d.Log).WithMany(p => p.LeadImportLogs)
                .HasForeignKey(d => d.LogId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("lead_import_log_ibfk_1");
        });

        modelBuilder.Entity<LeadStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Sid).HasName("PRIMARY");

            entity.ToTable("lead_status_history");

            entity.HasIndex(e => e.Lid, "LId");

            entity.HasIndex(e => e.Uid, "UId");

            entity.Property(e => e.Sid).HasColumnName("SId");
            entity.Property(e => e.Lid).HasColumnName("LId");
            entity.Property(e => e.NewStatus)
                .HasColumnType("enum('New','Contacted','Follow-up','Converted','Lost')")
                .HasColumnName("New_Status");
            entity.Property(e => e.OldStatus)
                .HasColumnType("enum('New','Contacted','Follow-up','Converted','Lost')")
                .HasColumnName("Old_Status");
            entity.Property(e => e.TimeOfChange)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("Time_Of_Change");
            entity.Property(e => e.Uid).HasColumnName("UId");

            entity.HasOne(d => d.LidNavigation).WithMany(p => p.LeadStatusHistories)
                .HasForeignKey(d => d.Lid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("lead_status_history_ibfk_1");

            entity.HasOne(d => d.UidNavigation).WithMany(p => p.LeadStatusHistories)
                .HasForeignKey(d => d.Uid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("lead_status_history_ibfk_2");
        });

        modelBuilder.Entity<LeadUpdateLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PRIMARY");

            entity.ToTable("lead_update_log");

            entity.HasIndex(e => e.Lid, "LId");

            entity.HasIndex(e => e.Uid, "UId");

            entity.Property(e => e.FieldUpdated)
                .HasMaxLength(255)
                .HasColumnName("Field_Updated");
            entity.Property(e => e.Lid).HasColumnName("LId");
            entity.Property(e => e.NewValue)
                .HasColumnType("text")
                .HasColumnName("New_Value");
            entity.Property(e => e.OldValue)
                .HasColumnType("text")
                .HasColumnName("Old_Value");
            entity.Property(e => e.Uid).HasColumnName("UId");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("Updated_At");

            entity.HasOne(d => d.LidNavigation).WithMany(p => p.LeadUpdateLogs)
                .HasForeignKey(d => d.Lid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("lead_update_log_ibfk_1");

            entity.HasOne(d => d.UidNavigation).WithMany(p => p.LeadUpdateLogs)
                .HasForeignKey(d => d.Uid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("lead_update_log_ibfk_2");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "Email").IsUnique();

            entity.HasIndex(e => e.ReportsTo, "Reports_To");

            entity.Property(e => e.Uid).HasColumnName("UId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("Created_At");
            entity.Property(e => e.DateOfJoining)
                .HasColumnType("timestamp")
                .HasColumnName("Date_Of_Joining");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.PhoneNo)
                .HasMaxLength(20)
                .HasColumnName("Phone_No");
            entity.Property(e => e.ReportsTo).HasColumnName("Reports_To");
            entity.Property(e => e.Role).HasColumnType("enum('Admin','Manager','Sales Representative')");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("timestamp")
                .HasColumnName("Updated_At");

            entity.HasOne(d => d.ReportsToNavigation).WithMany(p => p.InverseReportsToNavigation)
                .HasForeignKey(d => d.ReportsTo)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("users_ibfk_1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
