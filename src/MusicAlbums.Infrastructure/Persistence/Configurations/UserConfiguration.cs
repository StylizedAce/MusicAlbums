using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Name)
            .IsRequired()
            .HasMaxLength(100)
            .UseCollation("NOCASE");

        builder.Property(user => user.CreatedAt)
            .HasConversion(createdAt => createdAt.UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));

        builder.HasIndex(user => user.Name)
            .IsUnique();

        builder.HasMany(user => user.Albums)
            .WithOne(album => album.User)
            .HasForeignKey(album => album.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
