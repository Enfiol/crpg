using Crpg.Domain.Entities.BattleEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace Crpg.Persistence.Configurations;

public class CrpgGameEventConfiguration : IEntityTypeConfiguration<CrpgGameEvent>
{
    public void Configure(EntityTypeBuilder<CrpgGameEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .IsRequired(false);

        builder.HasIndex(e => new { e.CreatedAt, e.UserId });
        builder.HasIndex(e => e.Type);

        // Store dictionary as JSON in PostgreSQL
        builder.Property(e => e.EventData)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<Dictionary<CrpgGameEvent.EventField, string>>(v))
            .HasColumnType("jsonb");
    }
}
