using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrivo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "foods",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reference_unit = table.Column<int>(type: "integer", nullable: false),
                    carbohydrates_grams = table.Column<decimal>(type: "numeric", nullable: false),
                    fat_grams = table.Column<decimal>(type: "numeric", nullable: false),
                    kilocalories = table.Column<decimal>(type: "numeric", nullable: false),
                    protein_grams = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_foods", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "foods",
                schema: "public");
        }
    }
}
