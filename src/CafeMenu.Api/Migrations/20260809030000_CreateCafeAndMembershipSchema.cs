using System;
using CafeMenu.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CafeMenu.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CafeMenuDbContext))]
    [Migration("20260809030000_CreateCafeAndMembershipSchema")]
    public partial class CreateCafeAndMembershipSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cafe",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    logo_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cover_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cafe", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cafe_membership",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    app_user_id = table.Column<long>(type: "bigint", nullable: false),
                    cafe_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cafe_membership", x => x.id);
                    table.ForeignKey(
                        name: "fk_cafe_membership_app_user",
                        column: x => x.app_user_id,
                        principalSchema: "public",
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cafe_membership_cafe",
                        column: x => x.cafe_id,
                        principalSchema: "public",
                        principalTable: "cafe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cafe_membership_role",
                        column: x => x.role_id,
                        principalSchema: "public",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uk_cafe_slug",
                schema: "public",
                table: "cafe",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_cafe_membership_cafe",
                schema: "public",
                table: "cafe_membership",
                column: "cafe_id");

            migrationBuilder.CreateIndex(
                name: "idx_cafe_membership_role",
                schema: "public",
                table: "cafe_membership",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "uk_cafe_membership_app_user_cafe_active",
                schema: "public",
                table: "cafe_membership",
                columns: new[] { "app_user_id", "cafe_id" },
                unique: true,
                filter: "is_active = true AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cafe_membership",
                schema: "public");

            migrationBuilder.DropTable(
                name: "cafe",
                schema: "public");
        }
    }
}
