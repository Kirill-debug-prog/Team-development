using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ConsultantPlatform.Models.Entity;

public partial class MentiContext : DbContext
{
    public MentiContext()
    {
    }

    public MentiContext(DbContextOptions<MentiContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<ChatRoom> ChatRooms { get; set; }

    public virtual DbSet<Experience> Experiences { get; set; }

    public virtual DbSet<MentorCard> MentorCards { get; set; }

    public virtual DbSet<MentorCardsCategory> MentorCardsCategories { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Category_pkey");

            entity.ToTable("Category");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ChatRooms_pkey");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("ID");
            entity.Property(e => e.ClientId).HasColumnName("ClientID");
            entity.Property(e => e.MentorId).HasColumnName("MentorID");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Client).WithMany(p => p.ChatRoomClients)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_chat_client");

            entity.HasOne(d => d.Mentor).WithMany(p => p.ChatRoomMentors)
                .HasForeignKey(d => d.MentorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_chat_mentor");
        });

        modelBuilder.Entity<Experience>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Experience_pkey");

            entity.ToTable("Experience");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CompanyName).HasMaxLength(255);
            entity.Property(e => e.Position).HasMaxLength(255);

            entity.HasOne(d => d.MentorCard).WithMany(p => p.Experiences)
                .HasForeignKey(d => d.MentorCardId)
                .HasConstraintName("FK_Experience_MentorCard");
        });

        modelBuilder.Entity<MentorCard>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("MentorCards_pkey");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("ID");
            entity.Property(e => e.MentorId).HasColumnName("MentorID");
            entity.Property(e => e.PricePerHours).HasColumnType("money");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Mentor).WithMany(p => p.MentorCards)
                .HasForeignKey(d => d.MentorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_mentor");
        });

        modelBuilder.Entity<MentorCardsCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("MentorCards_Category_pkey");

            entity.ToTable("MentorCards_Category");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("ID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.MentorCardId).HasColumnName("MentorCardID");

            entity.HasOne(d => d.Category).WithMany(p => p.MentorCardsCategories)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_category");

            entity.HasOne(d => d.MentorCard).WithMany(p => p.MentorCardsCategories)
                .HasForeignKey(d => d.MentorCardId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_mentorcard");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Message_pkey");

            entity.ToTable("Message");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("ID");
            entity.Property(e => e.ChatRoomId).HasColumnName("ChatRoomID");
            entity.Property(e => e.DateSent)
                  .HasColumnType("timestamp with time zone") // или "timestamptz"
                  .HasColumnName("DateSent"); // Убедитесь, что имя колонки в БД совпадает или явно укажите его, если оно отличается от свойства
            entity.Property(e => e.Message1).HasColumnName("Message");
            entity.Property(e => e.SenderId).HasColumnName("SenderID");

            entity.HasOne(d => d.ChatRoom).WithMany(p => p.Messages)
                .HasForeignKey(d => d.ChatRoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_message_chatroom");

            entity.HasOne(d => d.Sender).WithMany(p => p.Messages)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_message_sender");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Users_pkey");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("ID");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.FirstName).HasMaxLength(200);
            entity.Property(e => e.LastName).HasMaxLength(200);
            entity.Property(e => e.Login).HasMaxLength(200);
            entity.Property(e => e.MiddleName).HasMaxLength(200);
            entity.Property(e => e.Password).HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
