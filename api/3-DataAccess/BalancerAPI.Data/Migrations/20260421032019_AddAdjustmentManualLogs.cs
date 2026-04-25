using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAdjustmentManualLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adjustment_manual_daily_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_weight = table.Column<int>(type: "integer", nullable: false),
                    new_weight = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adjustment_manual_daily_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "adjustment_manual_weekly_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    spec = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    previous_offset = table.Column<int>(type: "integer", nullable: false),
                    new_offset = table.Column<int>(type: "integer", nullable: false),
                    base_weight = table.Column<int>(type: "integer", nullable: false),
                    previous_spec_weight = table.Column<int>(type: "integer", nullable: false),
                    new_spec_weight = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adjustment_manual_weekly_log", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adjustment_manual_daily_log");

            migrationBuilder.DropTable(
                name: "adjustment_manual_weekly_log");
        }
    }
}
