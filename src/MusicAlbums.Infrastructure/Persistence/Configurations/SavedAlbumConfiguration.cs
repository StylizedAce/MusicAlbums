using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Infrastructure.Persistence.Configurations;

public sealed class SavedAlbumConfiguration : IEntityTypeConfiguration<SavedAlbum>
{
    public void Configure(EntityTypeBuilder<SavedAlbum> builder)
    {
        builder.ToTable("SavedAlbums");

        builder.HasKey(album => album.Id);

        builder.Property(album => album.ProviderName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(album => album.ProviderAlbumId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(album => album.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(album => album.ArtistName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(album => album.CoverImageUrl)
            .HasMaxLength(1000);

        builder.Property(album => album.SavedAt)
            .HasConversion(savedAt => savedAt.UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));

        builder.HasIndex(album => new { album.UserId, album.ProviderName, album.ProviderAlbumId })
            .IsUnique();

        builder.HasMany(album => album.Tracks)
            .WithOne()
            .HasForeignKey(track => track.SavedAlbumId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
