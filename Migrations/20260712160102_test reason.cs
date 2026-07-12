using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_PAYSIM.Migrations
{
    /// <inheritdoc />
    public partial class testreason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "HistoricalSms",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reason",
                table: "HistoricalSms");
        }
    }
}
