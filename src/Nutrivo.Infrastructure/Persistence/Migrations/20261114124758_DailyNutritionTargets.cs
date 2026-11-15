using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrivo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DailyNutritionTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_nutrition_targets",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    kilocalories =
                        table.Column<decimal>(type: "numeric", nullable: false),
                    protein_grams = table.Column<decimal>(
                        type: "numeric",
                        nullable: false
                    ),
                    carbohydrates_grams = table.Column<decimal>(
                        type: "numeric",
                        nullable: false
                    ),
                    fat_grams = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_nutrition_targets", x => x.id);
                    table.CheckConstraint(
                        "ck_daily_nutrition_targets_singleton",
                        "id = 1"
                    );
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_nutrition_targets",
                schema: "public"
            );
        }
    }
}
