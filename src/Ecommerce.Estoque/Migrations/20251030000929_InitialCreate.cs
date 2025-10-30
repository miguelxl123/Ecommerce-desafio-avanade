using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ecommerce.Estoque.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Sku = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "Price", "Sku", "StockQuantity", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Eletrônicos", new DateTime(2025, 10, 30, 0, 9, 29, 19, DateTimeKind.Utc).AddTicks(4873), "Smartphone top de linha com 256GB de armazenamento", true, "Smartphone Samsung Galaxy S23", 2499.99m, "SAMS23-256", 50, null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Informática", new DateTime(2025, 10, 30, 0, 9, 29, 19, DateTimeKind.Utc).AddTicks(5037), "Notebook profissional com Intel i7 e 16GB RAM", true, "Notebook Lenovo ThinkPad", 4299.99m, "LEN-TP-I7", 25, null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Áudio", new DateTime(2025, 10, 30, 0, 9, 29, 19, DateTimeKind.Utc).AddTicks(5040), "Fone com cancelamento de ruído ativo", true, "Fone de Ouvido Sony WH-1000XM5", 1599.99m, "SONY-WH1000", 100, null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "TV e Home Theater", new DateTime(2025, 10, 30, 0, 9, 29, 19, DateTimeKind.Utc).AddTicks(5043), "Smart TV com resolução 4K e HDR", true, "Smart TV 55\" 4K Samsung", 3299.99m, "SAMS-TV55-4K", 15, null },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "Games", new DateTime(2025, 10, 30, 0, 9, 29, 19, DateTimeKind.Utc).AddTicks(5045), "Console de videogame de última geração", true, "Console PlayStation 5", 4499.99m, "SONY-PS5", 8, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive",
                table: "Products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                table: "Products",
                column: "Sku",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
