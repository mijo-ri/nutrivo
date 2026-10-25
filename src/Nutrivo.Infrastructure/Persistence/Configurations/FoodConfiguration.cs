using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Infrastructure.Persistence.Configurations;

internal sealed class FoodConfiguration : IEntityTypeConfiguration<Food>
{
    public void Configure(EntityTypeBuilder<Food> builder)
    {
        builder.ToTable("foods", "public");
        builder.HasKey(food => food.Id);

        builder
            .Property(food => food.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => FoodId.Create(value).Value)
            .ValueGeneratedNever();

        builder
            .Property(food => food.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder
            .Property(food => food.Brand)
            .HasColumnName("brand")
            .HasMaxLength(200);

        builder
            .Property(food => food.ReferenceUnit)
            .HasColumnName("reference_unit")
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(food => food.ReferenceQuantity)
            .HasColumnName("reference_quantity")
            .HasColumnType("numeric")
            .IsRequired();

        builder
            .HasMany(food => food.ServingSizes)
            .WithOne()
            .HasForeignKey("FoodId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .Navigation(food => food.ServingSizes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(food => food.ServingSizes).AutoInclude();

        var nutrition = builder.ComplexProperty(food => food.NutritionalValues);
        nutrition.IsRequired();
        nutrition.UsePropertyAccessMode(PropertyAccessMode.Field);

        nutrition
            .Property(values => values.Kilocalories)
            .HasColumnName("kilocalories")
            .HasColumnType("numeric")
            .IsRequired();

        nutrition
            .Property(values => values.ProteinGrams)
            .HasColumnName("protein_grams")
            .HasColumnType("numeric")
            .IsRequired();

        nutrition
            .Property(values => values.CarbohydratesGrams)
            .HasColumnName("carbohydrates_grams")
            .HasColumnType("numeric")
            .IsRequired();

        nutrition
            .Property(values => values.FatGrams)
            .HasColumnName("fat_grams")
            .HasColumnType("numeric")
            .IsRequired();
    }
}
