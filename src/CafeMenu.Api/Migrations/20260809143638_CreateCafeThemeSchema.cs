using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CafeMenu.Api.Migrations
{
    /// <inheritdoc />
    public partial class CreateCafeThemeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cafe_theme",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cafe_id = table.Column<long>(type: "bigint", nullable: false),
                    primary_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValue: "#111827"),
                    secondary_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValue: "#F9FAFB"),
                    accent_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValue: "#D97706"),
                    background_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValue: "#FFFFFF"),
                    text_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValue: "#111827"),
                    welcome_title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    welcome_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    font_preset = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "SYSTEM"),
                    theme_preset = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "CLASSIC"),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cafe_theme", x => x.id);
                    table.ForeignKey(
                        name: "fk_cafe_theme_cafe",
                        column: x => x.cafe_id,
                        principalSchema: "public",
                        principalTable: "cafe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uk_cafe_theme_cafe",
                schema: "public",
                table: "cafe_theme",
                column: "cafe_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cafe_theme",
                schema: "public");
        }
    }
}
