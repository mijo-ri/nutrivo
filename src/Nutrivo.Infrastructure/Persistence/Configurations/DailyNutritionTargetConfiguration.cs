using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nutrivo.Domain.Targets;

namespace Nutrivo.Infrastructure.Persistence.Configurations;

internal sealed class
    DailyNutritionTargetConfiguration : IEntityTypeConfiguration<DailyNutritionTarget>
{
    public void Configure(EntityTypeBuilder<DailyNutritionTarget> builder)
    {
        builder.ToTable(
            "daily_nutrition_targets",
            "public",
            table =>
                table.HasCheckConstraint("ck_daily_nutrition_targets_singleton", "id = 1")
        );
        builder.HasKey(target => target.Id);
        builder.Property(target => target.Id).HasColumnName("id").ValueGeneratedNever();
        builder
            .Property(target => target.Kilocalories)
            .HasColumnName("kilocalories")
            .HasColumnType("numeric")
            .IsRequired();
        builder
            .Property(target => target.ProteinGrams)
            .HasColumnName("protein_grams")
            .HasColumnType("numeric")
            .IsRequired();
        builder
            .Property(target => target.CarbohydratesGrams)
            .HasColumnName("carbohydrates_grams")
            .HasColumnType("numeric")
            .IsRequired();
        builder
            .Property(target => target.FatGrams)
            .HasColumnName("fat_grams")
            .HasColumnType("numeric")
            .IsRequired();
    }
}
