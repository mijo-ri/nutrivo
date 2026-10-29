using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrivo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DiaryEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "diary_entries",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    meal_type = table.Column<int>(type: "integer", nullable: false),
                    food_id = table.Column<Guid>(type: "uuid", nullable: false),
                    food_name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    food_brand = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    food_reference_quantity = table.Column<decimal>(
                        type: "numeric",
                        nullable: false
                    ),
                    food_reference_unit = table.Column<int>(
                        type: "integer",
                        nullable: false
                    ),
                    food_kilocalories = table.Column<decimal>(
                        type: "numeric",
                        nullable: false
                    ),
                    food_protein_grams = table.Column<decimal>(
                        type: "numeric",
                        nullable: false
                    ),
                    food_carbohydrates_grams = table.Column<decimal>(
                        type: "numeric",
                        nullable: false
                    ),
                    food_fat_grams = table.Column<decimal>(
                        type: "numeric",
                        nullable: false
                    ),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    reference_unit = table.Column<int>(type: "integer", nullable: false),
                    serving_size_id = table.Column<Guid>(type: "uuid", nullable: true),
                    serving_quantity = table.Column<decimal>(
                        type: "numeric",
                        nullable: true
                    ),
                    serving_unit = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    serving_label = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    serving_equivalent_quantity = table.Column<decimal>(
                        type: "numeric",
                        nullable: true
                    ),
                    serving_count =
                        table.Column<decimal>(type: "numeric", nullable: true),
                    consumed_carbohydrates_grams = table.Column<decimal>(
                        type: "numeric",
                        nullable: false
                    ),
                    consumed_fat_grams = table.Column<decimal>(
                        type: "numeric",
                        nullable: false
                    ),
                    consumed_kilocalories = table.Column<decimal>(
                        type: "numeric",
                        nullable: false
                    ),
                    consumed_protein_grams = table.Column<decimal>(
                        type: "numeric",
                        nullable: false
                    )
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diary_entries", x => x.id);
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "diary_entries",
                schema: "public"
            );
        }
    }
}
