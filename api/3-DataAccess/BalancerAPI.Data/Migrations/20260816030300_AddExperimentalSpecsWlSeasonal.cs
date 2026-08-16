using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddExperimentalSpecsWlSeasonal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "experimental_specs_wl_seasonal",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    season_start_date = table.Column<int>(type: "integer", nullable: false),
                    pyromancer_wins = table.Column<int>(type: "integer", nullable: false),
                    pyromancer_losses = table.Column<int>(type: "integer", nullable: false),
                    pyromancer_kills = table.Column<int>(type: "integer", nullable: false),
                    pyromancer_deaths = table.Column<int>(type: "integer", nullable: false),
                    cryomancer_wins = table.Column<int>(type: "integer", nullable: false),
                    cryomancer_losses = table.Column<int>(type: "integer", nullable: false),
                    cryomancer_kills = table.Column<int>(type: "integer", nullable: false),
                    cryomancer_deaths = table.Column<int>(type: "integer", nullable: false),
                    aquamancer_wins = table.Column<int>(type: "integer", nullable: false),
                    aquamancer_losses = table.Column<int>(type: "integer", nullable: false),
                    aquamancer_kills = table.Column<int>(type: "integer", nullable: false),
                    aquamancer_deaths = table.Column<int>(type: "integer", nullable: false),
                    berserker_wins = table.Column<int>(type: "integer", nullable: false),
                    berserker_losses = table.Column<int>(type: "integer", nullable: false),
                    berserker_kills = table.Column<int>(type: "integer", nullable: false),
                    berserker_deaths = table.Column<int>(type: "integer", nullable: false),
                    defender_wins = table.Column<int>(type: "integer", nullable: false),
                    defender_losses = table.Column<int>(type: "integer", nullable: false),
                    defender_kills = table.Column<int>(type: "integer", nullable: false),
                    defender_deaths = table.Column<int>(type: "integer", nullable: false),
                    revenant_wins = table.Column<int>(type: "integer", nullable: false),
                    revenant_losses = table.Column<int>(type: "integer", nullable: false),
                    revenant_kills = table.Column<int>(type: "integer", nullable: false),
                    revenant_deaths = table.Column<int>(type: "integer", nullable: false),
                    avenger_wins = table.Column<int>(type: "integer", nullable: false),
                    avenger_losses = table.Column<int>(type: "integer", nullable: false),
                    avenger_kills = table.Column<int>(type: "integer", nullable: false),
                    avenger_deaths = table.Column<int>(type: "integer", nullable: false),
                    crusader_wins = table.Column<int>(type: "integer", nullable: false),
                    crusader_losses = table.Column<int>(type: "integer", nullable: false),
                    crusader_kills = table.Column<int>(type: "integer", nullable: false),
                    crusader_deaths = table.Column<int>(type: "integer", nullable: false),
                    protector_wins = table.Column<int>(type: "integer", nullable: false),
                    protector_losses = table.Column<int>(type: "integer", nullable: false),
                    protector_kills = table.Column<int>(type: "integer", nullable: false),
                    protector_deaths = table.Column<int>(type: "integer", nullable: false),
                    thunderlord_wins = table.Column<int>(type: "integer", nullable: false),
                    thunderlord_losses = table.Column<int>(type: "integer", nullable: false),
                    thunderlord_kills = table.Column<int>(type: "integer", nullable: false),
                    thunderlord_deaths = table.Column<int>(type: "integer", nullable: false),
                    spiritguard_wins = table.Column<int>(type: "integer", nullable: false),
                    spiritguard_losses = table.Column<int>(type: "integer", nullable: false),
                    spiritguard_kills = table.Column<int>(type: "integer", nullable: false),
                    spiritguard_deaths = table.Column<int>(type: "integer", nullable: false),
                    earthwarden_wins = table.Column<int>(type: "integer", nullable: false),
                    earthwarden_losses = table.Column<int>(type: "integer", nullable: false),
                    earthwarden_kills = table.Column<int>(type: "integer", nullable: false),
                    earthwarden_deaths = table.Column<int>(type: "integer", nullable: false),
                    assassin_wins = table.Column<int>(type: "integer", nullable: false),
                    assassin_losses = table.Column<int>(type: "integer", nullable: false),
                    assassin_kills = table.Column<int>(type: "integer", nullable: false),
                    assassin_deaths = table.Column<int>(type: "integer", nullable: false),
                    vindicator_wins = table.Column<int>(type: "integer", nullable: false),
                    vindicator_losses = table.Column<int>(type: "integer", nullable: false),
                    vindicator_kills = table.Column<int>(type: "integer", nullable: false),
                    vindicator_deaths = table.Column<int>(type: "integer", nullable: false),
                    apothecary_wins = table.Column<int>(type: "integer", nullable: false),
                    apothecary_losses = table.Column<int>(type: "integer", nullable: false),
                    apothecary_kills = table.Column<int>(type: "integer", nullable: false),
                    apothecary_deaths = table.Column<int>(type: "integer", nullable: false),
                    conjurer_wins = table.Column<int>(type: "integer", nullable: false),
                    conjurer_losses = table.Column<int>(type: "integer", nullable: false),
                    conjurer_kills = table.Column<int>(type: "integer", nullable: false),
                    conjurer_deaths = table.Column<int>(type: "integer", nullable: false),
                    sentinel_wins = table.Column<int>(type: "integer", nullable: false),
                    sentinel_losses = table.Column<int>(type: "integer", nullable: false),
                    sentinel_kills = table.Column<int>(type: "integer", nullable: false),
                    sentinel_deaths = table.Column<int>(type: "integer", nullable: false),
                    luminary_wins = table.Column<int>(type: "integer", nullable: false),
                    luminary_losses = table.Column<int>(type: "integer", nullable: false),
                    luminary_kills = table.Column<int>(type: "integer", nullable: false),
                    luminary_deaths = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experimental_specs_wl_seasonal", x => new { x.uuid, x.season_start_date });
                });

            var wlColumns = string.Join(",\n                    ", EnumerateWlColumnNames());
            var wlSelectColumns = string.Join(",\n                    ", EnumerateWlColumnNames().Select(c => $"t.{c}"));
            var currentSeasonColumns = BuildCurrentSeasonColumns();
            var seasonDeltaColumns = BuildSeasonDeltaColumns();

            migrationBuilder.Sql($"""
                CREATE OR REPLACE VIEW experimental_specs_wl_current_season AS
                SELECT
                    c.uuid,
                    {currentSeasonColumns}
                FROM experimental_specs_wl_total c
                LEFT JOIN experimental_specs_wl_seasonal s
                    ON c.uuid = s.uuid
                    AND s.season_start_date = (SELECT id FROM time_season ORDER BY id DESC OFFSET 0 LIMIT 1);
                """);

            migrationBuilder.Sql($"""
                CREATE VIEW experimental_specs_wl_season AS
                SELECT
                    curr.season_start_date - 1 AS season_start_date,
                    curr.uuid,
                    {seasonDeltaColumns}
                FROM experimental_specs_wl_seasonal curr
                LEFT JOIN experimental_specs_wl_seasonal prev
                    ON prev.uuid = curr.uuid
                   AND prev.season_start_date = curr.season_start_date - 1;
                """);

            migrationBuilder.Sql("""
                CREATE VIEW experimental_season_stats AS
                SELECT
                    uuid,
                    pyromancer_wins + cryomancer_wins + aquamancer_wins + berserker_wins + defender_wins + revenant_wins + avenger_wins + crusader_wins + protector_wins + thunderlord_wins + spiritguard_wins + earthwarden_wins + assassin_wins + vindicator_wins + apothecary_wins + conjurer_wins + sentinel_wins + luminary_wins AS wins,
                    pyromancer_losses + cryomancer_losses + aquamancer_losses + berserker_losses + defender_losses + revenant_losses + avenger_losses + crusader_losses + protector_losses + thunderlord_losses + spiritguard_losses + earthwarden_losses + assassin_losses + vindicator_losses + apothecary_losses + conjurer_losses + sentinel_losses + luminary_losses AS losses,
                    pyromancer_kills + cryomancer_kills + aquamancer_kills + berserker_kills + defender_kills + revenant_kills + avenger_kills + crusader_kills + protector_kills + thunderlord_kills + spiritguard_kills + earthwarden_kills + assassin_kills + vindicator_kills + apothecary_kills + conjurer_kills + sentinel_kills + luminary_kills AS kills,
                    pyromancer_deaths + cryomancer_deaths + aquamancer_deaths + berserker_deaths + defender_deaths + revenant_deaths + avenger_deaths + crusader_deaths + protector_deaths + thunderlord_deaths + spiritguard_deaths + earthwarden_deaths + assassin_deaths + vindicator_deaths + apothecary_deaths + conjurer_deaths + sentinel_deaths + luminary_deaths AS deaths
                FROM experimental_specs_wl_current_season;
                """);

            migrationBuilder.Sql("""
                CREATE VIEW experimental_season_stats_season AS
                SELECT
                    season_start_date,
                    uuid,
                    pyromancer_wins + cryomancer_wins + aquamancer_wins + berserker_wins + defender_wins + revenant_wins + avenger_wins + crusader_wins + protector_wins + thunderlord_wins + spiritguard_wins + earthwarden_wins + assassin_wins + vindicator_wins + apothecary_wins + conjurer_wins + sentinel_wins + luminary_wins AS wins,
                    pyromancer_losses + cryomancer_losses + aquamancer_losses + berserker_losses + defender_losses + revenant_losses + avenger_losses + crusader_losses + protector_losses + thunderlord_losses + spiritguard_losses + earthwarden_losses + assassin_losses + vindicator_losses + apothecary_losses + conjurer_losses + sentinel_losses + luminary_losses AS losses,
                    pyromancer_kills + cryomancer_kills + aquamancer_kills + berserker_kills + defender_kills + revenant_kills + avenger_kills + crusader_kills + protector_kills + thunderlord_kills + spiritguard_kills + earthwarden_kills + assassin_kills + vindicator_kills + apothecary_kills + conjurer_kills + sentinel_kills + luminary_kills AS kills,
                    pyromancer_deaths + cryomancer_deaths + aquamancer_deaths + berserker_deaths + defender_deaths + revenant_deaths + avenger_deaths + crusader_deaths + protector_deaths + thunderlord_deaths + spiritguard_deaths + earthwarden_deaths + assassin_deaths + vindicator_deaths + apothecary_deaths + conjurer_deaths + sentinel_deaths + luminary_deaths AS deaths
                FROM experimental_specs_wl_season;
                """);

            migrationBuilder.Sql($"""
                INSERT INTO experimental_specs_wl_seasonal (
                    uuid,
                    season_start_date,
                    {wlColumns}
                )
                SELECT
                    t.uuid,
                    s.id,
                    {wlSelectColumns}
                FROM experimental_specs_wl_total t
                CROSS JOIN (
                    SELECT id FROM time_season ORDER BY id DESC OFFSET 0 LIMIT 1
                ) s;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS experimental_season_stats_season;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS experimental_season_stats;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS experimental_specs_wl_season;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS experimental_specs_wl_current_season;");

            migrationBuilder.DropTable(
                name: "experimental_specs_wl_seasonal");
        }

        private static string BuildCurrentSeasonColumns()
        {
            var lines = new List<string>(72);
            foreach (var (spec, stat) in EnumerateSpecStats())
            {
                var col = $"{spec}_{stat}";
                lines.Add(
                    $"CASE WHEN s.uuid IS NOT NULL THEN c.{col} - COALESCE(s.{col}, 0) ELSE 0 END AS {col}");
            }

            return string.Join(",\n                    ", lines);
        }

        private static string BuildSeasonDeltaColumns()
        {
            var lines = new List<string>(72);
            foreach (var (spec, stat) in EnumerateSpecStats())
            {
                var col = $"{spec}_{stat}";
                lines.Add($"curr.{col} - COALESCE(prev.{col}, 0) AS {col}");
            }

            return string.Join(",\n                    ", lines);
        }

        private static IEnumerable<string> EnumerateWlColumnNames()
        {
            foreach (var (spec, stat) in EnumerateSpecStats())
            {
                yield return $"{spec}_{stat}";
            }
        }

        private static IEnumerable<(string Spec, string Stat)> EnumerateSpecStats()
        {
            var specs = new[]
            {
                "pyromancer", "cryomancer", "aquamancer", "berserker", "defender", "revenant",
                "avenger", "crusader", "protector", "thunderlord", "spiritguard", "earthwarden",
                "assassin", "vindicator", "apothecary", "conjurer", "sentinel", "luminary"
            };
            var stats = new[] { "wins", "losses", "kills", "deaths" };
            foreach (var spec in specs)
            {
                foreach (var stat in stats)
                {
                    yield return (spec, stat);
                }
            }
        }
    }
}
