using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Infrastructure.Persistence.Configurations;

public sealed class SavedTrackConfiguration : IEntityTypeConfiguration<SavedTrack>
{
    public void Configure(EntityTypeBuilder<SavedTrack> builder)
    {
        builder.ToTable("SavedTracks");

        builder.HasKey(track => track.Id);

        builder.Property(track => track.Title)
            .IsRequired()
            .HasMaxLength(500);
    }
}
