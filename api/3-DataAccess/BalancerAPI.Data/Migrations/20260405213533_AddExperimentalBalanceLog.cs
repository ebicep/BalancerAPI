using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddExperimentalBalanceLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "experimental_balance_log",
                columns: table => new
                {
                    balance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<string>(type: "character(24)", fixedLength: true, maxLength: 24, nullable: false),
                    balance = table.Column<string>(type: "jsonb", nullable: false),
                    meta = table.Column<string>(type: "jsonb", nullable: false),
                    posted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experimental_balance_log", x => new { x.balance_id, x.game_id });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "experimental_balance_log");
        }
    }
}
