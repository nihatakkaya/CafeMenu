using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CafeMenu.Api.Migrations
{
    /// <inheritdoc />
    public partial class CreateProductSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "uk_category_cafe_id",
                schema: "public",
                table: "category",
                columns: new[] { "cafe_id", "id" });

            migrationBuilder.CreateTable(
                name: "product",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cafe_id = table.Column<long>(type: "bigint", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_available = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_cafe",
                        column: x => x.cafe_id,
                        principalSchema: "public",
                        principalTable: "cafe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_category_cafe",
                        columns: x => new { x.cafe_id, x.category_id },
                        principalSchema: "public",
                        principalTable: "category",
                        principalColumns: new[] { "cafe_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_product_category",
                schema: "public",
                table: "product",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "idx_product_cafe",
                schema: "public",
                table: "product",
                column: "cafe_id");

            migrationBuilder.CreateIndex(
                name: "idx_product_cafe_category",
                schema: "public",
                table: "product",
                columns: new[] { "cafe_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "idx_product_cafe_category_display_order",
                schema: "public",
                table: "product",
                columns: new[] { "cafe_id", "category_id", "display_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product",
                schema: "public");

            migrationBuilder.DropUniqueConstraint(
                name: "uk_category_cafe_id",
                schema: "public",
                table: "category");
        }
    }
}
