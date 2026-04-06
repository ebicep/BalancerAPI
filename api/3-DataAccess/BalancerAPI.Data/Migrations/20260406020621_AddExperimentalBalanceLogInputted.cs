using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddExperimentalBalanceLogInputted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "inputted",
                table: "experimental_balance_log",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "inputted",
                table: "experimental_balance_log");
        }
    }
}
