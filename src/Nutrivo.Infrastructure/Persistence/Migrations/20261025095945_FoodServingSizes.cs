using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrivo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FoodServingSizes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "reference_quantity",
                schema: "public",
                table: "foods",
                type: "numeric",
                nullable: false,
                defaultValue: 100m
            );

            migrationBuilder.Sql(
                "ALTER TABLE public.foods ALTER COLUMN reference_quantity DROP DEFAULT;"
            );

            migrationBuilder.CreateTable(
                name: "food_serving_sizes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    label = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    equivalent_quantity = table.Column<decimal>(
                        type: "numeric",
                        nullable: false
                    ),
                    food_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_food_serving_sizes", x => x.id);
                    table.ForeignKey(
                        name: "FK_food_serving_sizes_foods_food_id",
                        column: x => x.food_id,
                        principalSchema: "public",
                        principalTable: "foods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_food_serving_sizes_food_id_position",
                schema: "public",
                table: "food_serving_sizes",
                columns: new[] { "food_id", "position" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                LOCK TABLE public.foods, public.food_serving_sizes IN ACCESS EXCLUSIVE MODE;
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM public.food_serving_sizes)
                        OR EXISTS (SELECT 1 FROM public.foods WHERE reference_quantity <> 100)
                    THEN
                        RAISE EXCEPTION 'Cannot roll back: serving sizes or non-100 reference quantities exist.'
                            USING ERRCODE = '23514';
                    END IF;
                END $$;
                """
            );

            migrationBuilder.DropTable(
                name: "food_serving_sizes",
                schema: "public"
            );

            migrationBuilder.DropColumn(
                name: "reference_quantity",
                schema: "public",
                table: "foods"
            );
        }
    }
}
