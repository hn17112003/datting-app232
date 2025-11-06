using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Dating_Manager.Models;

public partial class DatingManagerContext : DbContext
{
    public DatingManagerContext()
    {
    }

    public DatingManagerContext(DbContextOptions<DatingManagerContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Block> Blocks { get; set; }

    public virtual DbSet<Conversation> Conversations { get; set; }

    public virtual DbSet<Interest> Interests { get; set; }

    public virtual DbSet<Like> Likes { get; set; }

    public virtual DbSet<Match> Matches { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Photo> Photos { get; set; }

    public virtual DbSet<Profile> Profiles { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:MyCnn");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Block>(entity =>
        {
            entity.HasKey(e => new { e.BlockerId, e.BlockedId }).HasName("PK__Blocks__416BCA36F1BBBDBF");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Blocked).WithMany(p => p.BlockBlockeds)
                .HasForeignKey(d => d.BlockedId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Blocks__BlockedI__03F0984C");

            entity.HasOne(d => d.Blocker).WithMany(p => p.BlockBlockers)
                .HasForeignKey(d => d.BlockerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Blocks__BlockerI__02FC7413");
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Conversa__3214EC07B6335E42");

            entity.HasIndex(e => new { e.User1Id, e.User2Id }, "UQ_Conversations_Pair").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User1).WithMany(p => p.ConversationUser1s)
                .HasForeignKey(d => d.User1Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Conversations_User1");

            entity.HasOne(d => d.User2).WithMany(p => p.ConversationUser2s)
                .HasForeignKey(d => d.User2Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Conversations_User2");
        });

        modelBuilder.Entity<Interest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Interest__3214EC07F7E40CA8");

            entity.HasIndex(e => e.Name, "UQ__Interest__737584F68612B18E").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Like>(entity =>
        {
            entity.HasKey(e => new { e.SourceUserId, e.TargetUserId }).HasName("PK__Likes__D67C9E2EAB52F7CF");

            entity.HasIndex(e => e.SourceUserId, "IX_Likes_SourceUserId");

            entity.HasIndex(e => e.TargetUserId, "IX_Likes_TargetUserId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.SourceUser).WithMany(p => p.LikeSourceUsers)
                .HasForeignKey(d => d.SourceUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Likes_SourceUser");

            entity.HasOne(d => d.TargetUser).WithMany(p => p.LikeTargetUsers)
                .HasForeignKey(d => d.TargetUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Likes_TargetUser");
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Matches__3214EC07755B85E0");

            entity.HasIndex(e => e.User1Id, "IX_Matches_User1Id");

            entity.HasIndex(e => e.User2Id, "IX_Matches_User2Id");

            entity.HasIndex(e => new { e.User1Id, e.User2Id }, "UQ_Matches_Pair").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.User1).WithMany(p => p.MatchUser1s)
                .HasForeignKey(d => d.User1Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Matches_User1");

            entity.HasOne(d => d.User2).WithMany(p => p.MatchUser2s)
                .HasForeignKey(d => d.User2Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Matches_User2");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Messages__3214EC079E6FBA43");

            entity.HasIndex(e => new { e.ConversationId, e.SentAt }, "IX_Messages_ConversationId_SentAt");

            entity.HasIndex(e => e.RecipientId, "IX_Messages_RecipientId");

            entity.HasIndex(e => e.SenderId, "IX_Messages_SenderId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.SentAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Conversation).WithMany(p => p.Messages)
                .HasForeignKey(d => d.ConversationId)
                .HasConstraintName("FK_Messages_Conversations");

            entity.HasOne(d => d.Recipient).WithMany(p => p.MessageRecipients)
                .HasForeignKey(d => d.RecipientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Messages_Recipient");

            entity.HasOne(d => d.Sender).WithMany(p => p.MessageSenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Messages_Sender");
        });

        modelBuilder.Entity<Photo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Photos__3214EC07AC475CD4");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.PhotoType)
                .HasMaxLength(50)
                .HasDefaultValue("General");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Profile).WithMany(p => p.Photos)
                .HasForeignKey(d => d.ProfileId)
                .HasConstraintName("FK_Photos_Profiles");
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Profiles__3214EC070530BB2C");

            entity.HasIndex(e => e.UserId, "UQ__Profiles__1788CC4D32B1A0E2").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Bio).HasMaxLength(1500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(50);
            entity.Property(e => e.KnownAs).HasMaxLength(100);
            entity.Property(e => e.LookingForGender).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithOne(p => p.Profile)
                .HasForeignKey<Profile>(d => d.UserId)
                .HasConstraintName("FK_Profiles_Users");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC07AFD160C0");

            entity.HasIndex(e => e.Name, "UQ__Roles__737584F648717D10").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC076A28C90C");

            entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534E0F4CC22").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.LastActive).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.LastLoginIp).HasMaxLength(50);
            entity.Property(e => e.RoleId).HasDefaultValue(2);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_Users_Roles");

            entity.HasMany(d => d.Interests).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserInterest",
                    r => r.HasOne<Interest>().WithMany()
                        .HasForeignKey("InterestId")
                        .HasConstraintName("FK__UserInter__Inter__628FA481"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("FK__UserInter__UserI__619B8048"),
                    j =>
                    {
                        j.HasKey("UserId", "InterestId").HasName("PK__UserInte__7580FE8A304A450D");
                        j.ToTable("UserInterests");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
