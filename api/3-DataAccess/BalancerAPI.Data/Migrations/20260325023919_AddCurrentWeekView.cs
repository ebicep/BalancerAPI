using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentWeekView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE VIEW experimental_specs_wl_current_week AS
                SELECT
                    c.uuid,
                    CASE WHEN w.uuid IS NOT NULL THEN c.pyromancer_wins - COALESCE(w.pyromancer_wins, 0) ELSE 0 END AS pyromancer_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.pyromancer_losses - COALESCE(w.pyromancer_losses, 0) ELSE 0 END AS pyromancer_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.pyromancer_kills - COALESCE(w.pyromancer_kills, 0) ELSE 0 END AS pyromancer_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.pyromancer_deaths - COALESCE(w.pyromancer_deaths, 0) ELSE 0 END AS pyromancer_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.cryomancer_wins - COALESCE(w.cryomancer_wins, 0) ELSE 0 END AS cryomancer_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.cryomancer_losses - COALESCE(w.cryomancer_losses, 0) ELSE 0 END AS cryomancer_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.cryomancer_kills - COALESCE(w.cryomancer_kills, 0) ELSE 0 END AS cryomancer_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.cryomancer_deaths - COALESCE(w.cryomancer_deaths, 0) ELSE 0 END AS cryomancer_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.aquamancer_wins - COALESCE(w.aquamancer_wins, 0) ELSE 0 END AS aquamancer_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.aquamancer_losses - COALESCE(w.aquamancer_losses, 0) ELSE 0 END AS aquamancer_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.aquamancer_kills - COALESCE(w.aquamancer_kills, 0) ELSE 0 END AS aquamancer_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.aquamancer_deaths - COALESCE(w.aquamancer_deaths, 0) ELSE 0 END AS aquamancer_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.berserker_wins - COALESCE(w.berserker_wins, 0) ELSE 0 END AS berserker_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.berserker_losses - COALESCE(w.berserker_losses, 0) ELSE 0 END AS berserker_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.berserker_kills - COALESCE(w.berserker_kills, 0) ELSE 0 END AS berserker_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.berserker_deaths - COALESCE(w.berserker_deaths, 0) ELSE 0 END AS berserker_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.defender_wins - COALESCE(w.defender_wins, 0) ELSE 0 END AS defender_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.defender_losses - COALESCE(w.defender_losses, 0) ELSE 0 END AS defender_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.defender_kills - COALESCE(w.defender_kills, 0) ELSE 0 END AS defender_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.defender_deaths - COALESCE(w.defender_deaths, 0) ELSE 0 END AS defender_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.revenant_wins - COALESCE(w.revenant_wins, 0) ELSE 0 END AS revenant_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.revenant_losses - COALESCE(w.revenant_losses, 0) ELSE 0 END AS revenant_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.revenant_kills - COALESCE(w.revenant_kills, 0) ELSE 0 END AS revenant_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.revenant_deaths - COALESCE(w.revenant_deaths, 0) ELSE 0 END AS revenant_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.avenger_wins - COALESCE(w.avenger_wins, 0) ELSE 0 END AS avenger_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.avenger_losses - COALESCE(w.avenger_losses, 0) ELSE 0 END AS avenger_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.avenger_kills - COALESCE(w.avenger_kills, 0) ELSE 0 END AS avenger_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.avenger_deaths - COALESCE(w.avenger_deaths, 0) ELSE 0 END AS avenger_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.crusader_wins - COALESCE(w.crusader_wins, 0) ELSE 0 END AS crusader_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.crusader_losses - COALESCE(w.crusader_losses, 0) ELSE 0 END AS crusader_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.crusader_kills - COALESCE(w.crusader_kills, 0) ELSE 0 END AS crusader_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.crusader_deaths - COALESCE(w.crusader_deaths, 0) ELSE 0 END AS crusader_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.protector_wins - COALESCE(w.protector_wins, 0) ELSE 0 END AS protector_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.protector_losses - COALESCE(w.protector_losses, 0) ELSE 0 END AS protector_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.protector_kills - COALESCE(w.protector_kills, 0) ELSE 0 END AS protector_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.protector_deaths - COALESCE(w.protector_deaths, 0) ELSE 0 END AS protector_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.thunderlord_wins - COALESCE(w.thunderlord_wins, 0) ELSE 0 END AS thunderlord_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.thunderlord_losses - COALESCE(w.thunderlord_losses, 0) ELSE 0 END AS thunderlord_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.thunderlord_kills - COALESCE(w.thunderlord_kills, 0) ELSE 0 END AS thunderlord_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.thunderlord_deaths - COALESCE(w.thunderlord_deaths, 0) ELSE 0 END AS thunderlord_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.spiritguard_wins - COALESCE(w.spiritguard_wins, 0) ELSE 0 END AS spiritguard_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.spiritguard_losses - COALESCE(w.spiritguard_losses, 0) ELSE 0 END AS spiritguard_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.spiritguard_kills - COALESCE(w.spiritguard_kills, 0) ELSE 0 END AS spiritguard_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.spiritguard_deaths - COALESCE(w.spiritguard_deaths, 0) ELSE 0 END AS spiritguard_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.earthwarden_wins - COALESCE(w.earthwarden_wins, 0) ELSE 0 END AS earthwarden_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.earthwarden_losses - COALESCE(w.earthwarden_losses, 0) ELSE 0 END AS earthwarden_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.earthwarden_kills - COALESCE(w.earthwarden_kills, 0) ELSE 0 END AS earthwarden_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.earthwarden_deaths - COALESCE(w.earthwarden_deaths, 0) ELSE 0 END AS earthwarden_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.assassin_wins - COALESCE(w.assassin_wins, 0) ELSE 0 END AS assassin_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.assassin_losses - COALESCE(w.assassin_losses, 0) ELSE 0 END AS assassin_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.assassin_kills - COALESCE(w.assassin_kills, 0) ELSE 0 END AS assassin_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.assassin_deaths - COALESCE(w.assassin_deaths, 0) ELSE 0 END AS assassin_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.vindicator_wins - COALESCE(w.vindicator_wins, 0) ELSE 0 END AS vindicator_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.vindicator_losses - COALESCE(w.vindicator_losses, 0) ELSE 0 END AS vindicator_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.vindicator_kills - COALESCE(w.vindicator_kills, 0) ELSE 0 END AS vindicator_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.vindicator_deaths - COALESCE(w.vindicator_deaths, 0) ELSE 0 END AS vindicator_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.apothecary_wins - COALESCE(w.apothecary_wins, 0) ELSE 0 END AS apothecary_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.apothecary_losses - COALESCE(w.apothecary_losses, 0) ELSE 0 END AS apothecary_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.apothecary_kills - COALESCE(w.apothecary_kills, 0) ELSE 0 END AS apothecary_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.apothecary_deaths - COALESCE(w.apothecary_deaths, 0) ELSE 0 END AS apothecary_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.conjurer_wins - COALESCE(w.conjurer_wins, 0) ELSE 0 END AS conjurer_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.conjurer_losses - COALESCE(w.conjurer_losses, 0) ELSE 0 END AS conjurer_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.conjurer_kills - COALESCE(w.conjurer_kills, 0) ELSE 0 END AS conjurer_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.conjurer_deaths - COALESCE(w.conjurer_deaths, 0) ELSE 0 END AS conjurer_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.sentinel_wins - COALESCE(w.sentinel_wins, 0) ELSE 0 END AS sentinel_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.sentinel_losses - COALESCE(w.sentinel_losses, 0) ELSE 0 END AS sentinel_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.sentinel_kills - COALESCE(w.sentinel_kills, 0) ELSE 0 END AS sentinel_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.sentinel_deaths - COALESCE(w.sentinel_deaths, 0) ELSE 0 END AS sentinel_deaths,
                    CASE WHEN w.uuid IS NOT NULL THEN c.luminary_wins - COALESCE(w.luminary_wins, 0) ELSE 0 END AS luminary_wins,
                    CASE WHEN w.uuid IS NOT NULL THEN c.luminary_losses - COALESCE(w.luminary_losses, 0) ELSE 0 END AS luminary_losses,
                    CASE WHEN w.uuid IS NOT NULL THEN c.luminary_kills - COALESCE(w.luminary_kills, 0) ELSE 0 END AS luminary_kills,
                    CASE WHEN w.uuid IS NOT NULL THEN c.luminary_deaths - COALESCE(w.luminary_deaths, 0) ELSE 0 END AS luminary_deaths
                FROM experimental_specs_wl c
                LEFT JOIN experimental_specs_wl_weekly w
                    ON c.uuid = w.uuid
                    AND w.week_start_date = (SELECT MAX(week_start_date) FROM experimental_specs_wl_weekly);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS experimental_specs_wl_current_week;");
        }
    }
}
