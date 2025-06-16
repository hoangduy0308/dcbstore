using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCBStore.Migrations
{
    /// <inheritdoc />
    public partial class RenameOrderTotalAmountToTotal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "Orders",
                newName: "Total");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Total",
                table: "Orders",
                newName: "TotalAmount");
        }
    }
}
