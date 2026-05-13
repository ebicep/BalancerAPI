using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddExperimentalSpecBans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "experimental_spec_bans",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    pyromancer = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    cryomancer = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    aquamancer = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    berserker = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    defender = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    revenant = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    avenger = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    crusader = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    protector = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    thunderlord = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    spiritguard = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    earthwarden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    assassin = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    vindicator = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    apothecary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    conjurer = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sentinel = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    luminary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experimental_spec_bans", x => x.uuid);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "experimental_spec_bans");
        }
    }
}
