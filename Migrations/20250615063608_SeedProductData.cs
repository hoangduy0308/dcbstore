using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DCBStore.Migrations
{
    /// <inheritdoc />
    public partial class SeedProductData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Description", "ImageUrl", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "Laptop doanh nhân mạnh mẽ, bền bỉ.", "/images/laptop-thinkpad.jpg", "Laptop ThinkPad T14", 25000000m },
                    { 2, "Cuốn sách bán chạy nhất mọi thời đại của Paulo Coelho.", "/images/nha-gia-kim.jpg", "Tiểu thuyết 'Nhà Giả Kim'", 69000m },
                    { 3, "Hương thơm cổ điển và quyến rũ cho phái nữ.", "/images/chanel-no5.jpg", "Nước hoa Chanel No. 5", 3500000m },
                    { 4, "Tai nghe chống ồn chủ động hàng đầu thế giới.", "/images/sony-headphone.jpg", "Tai nghe Sony WH-1000XM5", 8500000m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
