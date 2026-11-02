using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nutrivo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DiaryEntryCaptureTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                schema: "public",
                table: "diary_entries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "public",
                table: "diary_entries"
            );
        }
    }
}
