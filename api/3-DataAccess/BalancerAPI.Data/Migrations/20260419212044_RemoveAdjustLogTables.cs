using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAdjustLogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adjust_log_daily");

            migrationBuilder.DropTable(
                name: "adjust_log_weekly");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adjust_log_daily",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    adjustment = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adjust_log_daily", x => x.uuid);
                });

            migrationBuilder.CreateTable(
                name: "adjust_log_weekly",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    apothecary = table.Column<int>(type: "integer", nullable: false),
                    aquamancer = table.Column<int>(type: "integer", nullable: false),
                    assassin = table.Column<int>(type: "integer", nullable: false),
                    avenger = table.Column<int>(type: "integer", nullable: false),
                    berserker = table.Column<int>(type: "integer", nullable: false),
                    conjurer = table.Column<int>(type: "integer", nullable: false),
                    crusader = table.Column<int>(type: "integer", nullable: false),
                    cryomancer = table.Column<int>(type: "integer", nullable: false),
                    defender = table.Column<int>(type: "integer", nullable: false),
                    earthwarden = table.Column<int>(type: "integer", nullable: false),
                    luminary = table.Column<int>(type: "integer", nullable: false),
                    protector = table.Column<int>(type: "integer", nullable: false),
                    pyromancer = table.Column<int>(type: "integer", nullable: false),
                    revenant = table.Column<int>(type: "integer", nullable: false),
                    sentinel = table.Column<int>(type: "integer", nullable: false),
                    spiritguard = table.Column<int>(type: "integer", nullable: false),
                    thunderlord = table.Column<int>(type: "integer", nullable: false),
                    vindicator = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adjust_log_weekly", x => x.uuid);
                });
        }
    }
}
