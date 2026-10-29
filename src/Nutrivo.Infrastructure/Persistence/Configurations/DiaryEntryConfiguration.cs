using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Infrastructure.Persistence.Configurations;

internal sealed class DiaryEntryConfiguration : IEntityTypeConfiguration<DiaryEntry>
{
    public void Configure(EntityTypeBuilder<DiaryEntry> builder)
    {
        builder.ToTable("diary_entries", "public");
        builder.HasKey(entry => entry.Id);
        builder
            .Property(entry => entry.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => DiaryEntryId.Create(value).Value)
            .ValueGeneratedNever();
        builder.Property(entry => entry.Date).HasColumnName("date");
        builder
            .Property(entry => entry.MealType)
            .HasColumnName("meal_type")
            .HasConversion<int>();
        builder
            .Property(entry => entry.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("numeric");
        builder
            .Property(entry => entry.ReferenceUnit)
            .HasColumnName("reference_unit")
            .HasConversion<int>();
        builder
            .Property(entry => entry.ServingCount)
            .HasColumnName("serving_count")
            .HasColumnType("numeric");

        ConfigureNutrition(
            builder.ComplexProperty(entry => entry.NutritionalValues),
            "consumed_"
        );

        builder.OwnsOne(
            entry => entry.FoodSnapshot,
            snapshot =>
            {
                snapshot
                    .Property(food => food.FoodId)
                    .HasColumnName("food_id")
                    .HasConversion(id => id.Value, value => FoodId.Create(value).Value);
                snapshot
                    .Property(food => food.Name)
                    .HasColumnName("food_name")
                    .HasMaxLength(200)
                    .IsRequired();
                snapshot
                    .Property(food => food.Brand)
                    .HasColumnName("food_brand")
                    .HasMaxLength(200);
                snapshot
                    .Property(food => food.ReferenceQuantity)
                    .HasColumnName("food_reference_quantity")
                    .HasColumnType("numeric");
                snapshot
                    .Property(food => food.ReferenceUnit)
                    .HasColumnName("food_reference_unit")
                    .HasConversion<int>();
                snapshot.OwnsOne(
                    food => food.NutritionalValues,
                    nutrition =>
                    {
                        nutrition
                            .Property(values => values.Kilocalories)
                            .HasColumnName("food_kilocalories")
                            .HasColumnType("numeric");
                        nutrition
                            .Property(values => values.ProteinGrams)
                            .HasColumnName("food_protein_grams")
                            .HasColumnType("numeric");
                        nutrition
                            .Property(values => values.CarbohydratesGrams)
                            .HasColumnName("food_carbohydrates_grams")
                            .HasColumnType("numeric");
                        nutrition
                            .Property(values => values.FatGrams)
                            .HasColumnName("food_fat_grams")
                            .HasColumnType("numeric");
                    }
                );
                snapshot
                    .Navigation(food => food.NutritionalValues)
                    .IsRequired()
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
            }
        );
        builder
            .Navigation(entry => entry.FoodSnapshot)
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsOne(
            entry => entry.ServingSizeSnapshot,
            snapshot =>
            {
                snapshot
                    .Property(serving => serving.ServingSizeId)
                    .HasColumnName("serving_size_id")
                    .HasConversion(
                        id => id.Value,
                        value => ServingSizeId.Create(value).Value
                    )
                    .IsRequired();
                snapshot
                    .Property(serving => serving.Quantity)
                    .HasColumnName("serving_quantity")
                    .HasColumnType("numeric");
                snapshot
                    .Property(serving => serving.Unit)
                    .HasColumnName("serving_unit")
                    .HasMaxLength(200)
                    .IsRequired();
                snapshot
                    .Property(serving => serving.Label)
                    .HasColumnName("serving_label")
                    .HasMaxLength(200);
                snapshot
                    .Property(serving => serving.EquivalentQuantity)
                    .HasColumnName("serving_equivalent_quantity")
                    .HasColumnType("numeric");
            }
        );
        builder
            .Navigation(entry => entry.ServingSizeSnapshot)
            .IsRequired(false)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureNutrition(
        ComplexPropertyBuilder<NutritionalValues> nutrition,
        string prefix
    )
    {
        nutrition.IsRequired();
        nutrition.UsePropertyAccessMode(PropertyAccessMode.Field);
        nutrition
            .Property(values => values.Kilocalories)
            .HasColumnName(prefix + "kilocalories")
            .HasColumnType("numeric");
        nutrition
            .Property(values => values.ProteinGrams)
            .HasColumnName(prefix + "protein_grams")
            .HasColumnType("numeric");
        nutrition
            .Property(values => values.CarbohydratesGrams)
            .HasColumnName(prefix + "carbohydrates_grams")
            .HasColumnType("numeric");
        nutrition
            .Property(values => values.FatGrams)
            .HasColumnName(prefix + "fat_grams")
            .HasColumnType("numeric");
    }
}
