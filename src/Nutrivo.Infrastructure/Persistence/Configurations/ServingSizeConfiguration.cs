using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Infrastructure.Persistence.Configurations;

internal sealed class ServingSizeConfiguration : IEntityTypeConfiguration<ServingSize>
{
    public void Configure(EntityTypeBuilder<ServingSize> builder)
    {
        builder.ToTable("food_serving_sizes", "public");
        builder.HasKey(serving => serving.Id);
        builder
            .Property(serving => serving.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => ServingSizeId.Create(value).Value)
            .ValueGeneratedNever();
        builder
            .Property<FoodId>("FoodId")
            .HasColumnName("food_id")
            .HasConversion(id => id.Value, value => FoodId.Create(value).Value);
        builder.Property(serving => serving.Position).HasColumnName("position");
        builder.HasIndex("FoodId", nameof(ServingSize.Position)).IsUnique();
        builder
            .Property(serving => serving.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("numeric");
        builder
            .Property(serving => serving.Unit)
            .HasColumnName("unit")
            .HasMaxLength(200)
            .IsRequired();
        builder
            .Property(serving => serving.Label)
            .HasColumnName("label")
            .HasMaxLength(200);
        builder
            .Property(serving => serving.EquivalentQuantity)
            .HasColumnName("equivalent_quantity")
            .HasColumnType("numeric");
    }
}
