using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddExperimentalSpecsWlDayView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS experimental_daily_stats_day(integer);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS experimental_specs_wl_day(integer);");
            migrationBuilder.Sql("DROP VIEW IF EXISTS experimental_daily_stats_day;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS experimental_specs_wl_day;");

            var wlDayColumns = BuildWlDayDeltaColumns();

            migrationBuilder.Sql($"""
                CREATE VIEW experimental_specs_wl_day AS
                SELECT
                    curr.day_start_date - 1 AS day_start_date,
                    curr.uuid,
                    {wlDayColumns}
                FROM experimental_specs_wl_daily curr
                LEFT JOIN experimental_specs_wl_daily prev
                    ON prev.uuid = curr.uuid
                   AND prev.day_start_date = curr.day_start_date - 1;
                """);

            migrationBuilder.Sql("""
                CREATE VIEW experimental_daily_stats_day AS
                SELECT
                    day_start_date,
                    uuid,
                    pyromancer_wins + cryomancer_wins + aquamancer_wins + berserker_wins + defender_wins + revenant_wins + avenger_wins + crusader_wins + protector_wins + thunderlord_wins + spiritguard_wins + earthwarden_wins + assassin_wins + vindicator_wins + apothecary_wins + conjurer_wins + sentinel_wins + luminary_wins AS wins,
                    pyromancer_losses + cryomancer_losses + aquamancer_losses + berserker_losses + defender_losses + revenant_losses + avenger_losses + crusader_losses + protector_losses + thunderlord_losses + spiritguard_losses + earthwarden_losses + assassin_losses + vindicator_losses + apothecary_losses + conjurer_losses + sentinel_losses + luminary_losses AS losses,
                    pyromancer_kills + cryomancer_kills + aquamancer_kills + berserker_kills + defender_kills + revenant_kills + avenger_kills + crusader_kills + protector_kills + thunderlord_kills + spiritguard_kills + earthwarden_kills + assassin_kills + vindicator_kills + apothecary_kills + conjurer_kills + sentinel_kills + luminary_kills AS kills,
                    pyromancer_deaths + cryomancer_deaths + aquamancer_deaths + berserker_deaths + defender_deaths + revenant_deaths + avenger_deaths + crusader_deaths + protector_deaths + thunderlord_deaths + spiritguard_deaths + earthwarden_deaths + assassin_deaths + vindicator_deaths + apothecary_deaths + conjurer_deaths + sentinel_deaths + luminary_deaths AS deaths
                FROM experimental_specs_wl_day;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS experimental_daily_stats_day;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS experimental_specs_wl_day;");
        }

        private static string BuildWlDayDeltaColumns()
        {
            var lines = new List<string>(72);
            foreach (var (spec, stat) in EnumerateSpecStats())
            {
                lines.Add(
                    $"curr.{spec}_{stat} - COALESCE(prev.{spec}_{stat}, 0) AS {spec}_{stat}");
            }

            return string.Join(",\n                    ", lines);
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
