using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_PAYSIM.Migrations
{
    /// <inheritdoc />
    public partial class updatehistorical : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActionKey",
                table: "Historical");

            migrationBuilder.RenameColumn(
                name: "NumberCustomer",
                table: "Historical",
                newName: "Name_developer");

            migrationBuilder.AddColumn<decimal>(
                name: "Balance_seller",
                table: "HistoricalSms",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Created_at",
                table: "HistoricalSms",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Id_developer",
                table: "HistoricalSms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Id_user",
                table: "HistoricalSms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name_customer",
                table: "HistoricalSms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Created_at",
                table: "Historical",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Balance_seller",
                table: "HistoricalSms");

            migrationBuilder.DropColumn(
                name: "Created_at",
                table: "HistoricalSms");

            migrationBuilder.DropColumn(
                name: "Id_developer",
                table: "HistoricalSms");

            migrationBuilder.DropColumn(
                name: "Id_user",
                table: "HistoricalSms");

            migrationBuilder.DropColumn(
                name: "Name_customer",
                table: "HistoricalSms");

            migrationBuilder.DropColumn(
                name: "Created_at",
                table: "Historical");

            migrationBuilder.RenameColumn(
                name: "Name_developer",
                table: "Historical",
                newName: "NumberCustomer");

            migrationBuilder.AddColumn<string>(
                name: "ActionKey",
                table: "Historical",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
