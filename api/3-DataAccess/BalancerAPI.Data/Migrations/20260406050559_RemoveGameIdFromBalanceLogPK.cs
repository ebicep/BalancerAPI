using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGameIdFromBalanceLogPK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_experimental_balance_log",
                table: "experimental_balance_log");

            migrationBuilder.AlterColumn<string>(
                name: "game_id",
                table: "experimental_balance_log",
                type: "character(24)",
                fixedLength: true,
                maxLength: 24,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character(24)",
                oldFixedLength: true,
                oldMaxLength: 24);

            migrationBuilder.AddPrimaryKey(
                name: "PK_experimental_balance_log",
                table: "experimental_balance_log",
                column: "balance_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_experimental_balance_log",
                table: "experimental_balance_log");

            migrationBuilder.AlterColumn<string>(
                name: "game_id",
                table: "experimental_balance_log",
                type: "character(24)",
                fixedLength: true,
                maxLength: 24,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character(24)",
                oldFixedLength: true,
                oldMaxLength: 24,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_experimental_balance_log",
                table: "experimental_balance_log",
                columns: new[] { "balance_id", "game_id" });
        }
    }
}
