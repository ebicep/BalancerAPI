using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentDayView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE VIEW experimental_specs_wl_current_day AS
                SELECT
                    c.uuid,
                    CASE WHEN d.uuid IS NOT NULL THEN c.pyromancer_wins - COALESCE(d.pyromancer_wins, 0) ELSE 0 END AS pyromancer_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.pyromancer_losses - COALESCE(d.pyromancer_losses, 0) ELSE 0 END AS pyromancer_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.pyromancer_kills - COALESCE(d.pyromancer_kills, 0) ELSE 0 END AS pyromancer_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.pyromancer_deaths - COALESCE(d.pyromancer_deaths, 0) ELSE 0 END AS pyromancer_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.cryomancer_wins - COALESCE(d.cryomancer_wins, 0) ELSE 0 END AS cryomancer_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.cryomancer_losses - COALESCE(d.cryomancer_losses, 0) ELSE 0 END AS cryomancer_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.cryomancer_kills - COALESCE(d.cryomancer_kills, 0) ELSE 0 END AS cryomancer_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.cryomancer_deaths - COALESCE(d.cryomancer_deaths, 0) ELSE 0 END AS cryomancer_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.aquamancer_wins - COALESCE(d.aquamancer_wins, 0) ELSE 0 END AS aquamancer_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.aquamancer_losses - COALESCE(d.aquamancer_losses, 0) ELSE 0 END AS aquamancer_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.aquamancer_kills - COALESCE(d.aquamancer_kills, 0) ELSE 0 END AS aquamancer_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.aquamancer_deaths - COALESCE(d.aquamancer_deaths, 0) ELSE 0 END AS aquamancer_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.berserker_wins - COALESCE(d.berserker_wins, 0) ELSE 0 END AS berserker_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.berserker_losses - COALESCE(d.berserker_losses, 0) ELSE 0 END AS berserker_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.berserker_kills - COALESCE(d.berserker_kills, 0) ELSE 0 END AS berserker_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.berserker_deaths - COALESCE(d.berserker_deaths, 0) ELSE 0 END AS berserker_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.defender_wins - COALESCE(d.defender_wins, 0) ELSE 0 END AS defender_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.defender_losses - COALESCE(d.defender_losses, 0) ELSE 0 END AS defender_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.defender_kills - COALESCE(d.defender_kills, 0) ELSE 0 END AS defender_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.defender_deaths - COALESCE(d.defender_deaths, 0) ELSE 0 END AS defender_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.revenant_wins - COALESCE(d.revenant_wins, 0) ELSE 0 END AS revenant_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.revenant_losses - COALESCE(d.revenant_losses, 0) ELSE 0 END AS revenant_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.revenant_kills - COALESCE(d.revenant_kills, 0) ELSE 0 END AS revenant_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.revenant_deaths - COALESCE(d.revenant_deaths, 0) ELSE 0 END AS revenant_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.avenger_wins - COALESCE(d.avenger_wins, 0) ELSE 0 END AS avenger_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.avenger_losses - COALESCE(d.avenger_losses, 0) ELSE 0 END AS avenger_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.avenger_kills - COALESCE(d.avenger_kills, 0) ELSE 0 END AS avenger_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.avenger_deaths - COALESCE(d.avenger_deaths, 0) ELSE 0 END AS avenger_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.crusader_wins - COALESCE(d.crusader_wins, 0) ELSE 0 END AS crusader_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.crusader_losses - COALESCE(d.crusader_losses, 0) ELSE 0 END AS crusader_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.crusader_kills - COALESCE(d.crusader_kills, 0) ELSE 0 END AS crusader_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.crusader_deaths - COALESCE(d.crusader_deaths, 0) ELSE 0 END AS crusader_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.protector_wins - COALESCE(d.protector_wins, 0) ELSE 0 END AS protector_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.protector_losses - COALESCE(d.protector_losses, 0) ELSE 0 END AS protector_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.protector_kills - COALESCE(d.protector_kills, 0) ELSE 0 END AS protector_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.protector_deaths - COALESCE(d.protector_deaths, 0) ELSE 0 END AS protector_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.thunderlord_wins - COALESCE(d.thunderlord_wins, 0) ELSE 0 END AS thunderlord_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.thunderlord_losses - COALESCE(d.thunderlord_losses, 0) ELSE 0 END AS thunderlord_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.thunderlord_kills - COALESCE(d.thunderlord_kills, 0) ELSE 0 END AS thunderlord_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.thunderlord_deaths - COALESCE(d.thunderlord_deaths, 0) ELSE 0 END AS thunderlord_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.spiritguard_wins - COALESCE(d.spiritguard_wins, 0) ELSE 0 END AS spiritguard_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.spiritguard_losses - COALESCE(d.spiritguard_losses, 0) ELSE 0 END AS spiritguard_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.spiritguard_kills - COALESCE(d.spiritguard_kills, 0) ELSE 0 END AS spiritguard_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.spiritguard_deaths - COALESCE(d.spiritguard_deaths, 0) ELSE 0 END AS spiritguard_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.earthwarden_wins - COALESCE(d.earthwarden_wins, 0) ELSE 0 END AS earthwarden_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.earthwarden_losses - COALESCE(d.earthwarden_losses, 0) ELSE 0 END AS earthwarden_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.earthwarden_kills - COALESCE(d.earthwarden_kills, 0) ELSE 0 END AS earthwarden_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.earthwarden_deaths - COALESCE(d.earthwarden_deaths, 0) ELSE 0 END AS earthwarden_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.assassin_wins - COALESCE(d.assassin_wins, 0) ELSE 0 END AS assassin_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.assassin_losses - COALESCE(d.assassin_losses, 0) ELSE 0 END AS assassin_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.assassin_kills - COALESCE(d.assassin_kills, 0) ELSE 0 END AS assassin_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.assassin_deaths - COALESCE(d.assassin_deaths, 0) ELSE 0 END AS assassin_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.vindicator_wins - COALESCE(d.vindicator_wins, 0) ELSE 0 END AS vindicator_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.vindicator_losses - COALESCE(d.vindicator_losses, 0) ELSE 0 END AS vindicator_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.vindicator_kills - COALESCE(d.vindicator_kills, 0) ELSE 0 END AS vindicator_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.vindicator_deaths - COALESCE(d.vindicator_deaths, 0) ELSE 0 END AS vindicator_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.apothecary_wins - COALESCE(d.apothecary_wins, 0) ELSE 0 END AS apothecary_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.apothecary_losses - COALESCE(d.apothecary_losses, 0) ELSE 0 END AS apothecary_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.apothecary_kills - COALESCE(d.apothecary_kills, 0) ELSE 0 END AS apothecary_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.apothecary_deaths - COALESCE(d.apothecary_deaths, 0) ELSE 0 END AS apothecary_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.conjurer_wins - COALESCE(d.conjurer_wins, 0) ELSE 0 END AS conjurer_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.conjurer_losses - COALESCE(d.conjurer_losses, 0) ELSE 0 END AS conjurer_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.conjurer_kills - COALESCE(d.conjurer_kills, 0) ELSE 0 END AS conjurer_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.conjurer_deaths - COALESCE(d.conjurer_deaths, 0) ELSE 0 END AS conjurer_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.sentinel_wins - COALESCE(d.sentinel_wins, 0) ELSE 0 END AS sentinel_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.sentinel_losses - COALESCE(d.sentinel_losses, 0) ELSE 0 END AS sentinel_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.sentinel_kills - COALESCE(d.sentinel_kills, 0) ELSE 0 END AS sentinel_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.sentinel_deaths - COALESCE(d.sentinel_deaths, 0) ELSE 0 END AS sentinel_deaths,
                    CASE WHEN d.uuid IS NOT NULL THEN c.luminary_wins - COALESCE(d.luminary_wins, 0) ELSE 0 END AS luminary_wins,
                    CASE WHEN d.uuid IS NOT NULL THEN c.luminary_losses - COALESCE(d.luminary_losses, 0) ELSE 0 END AS luminary_losses,
                    CASE WHEN d.uuid IS NOT NULL THEN c.luminary_kills - COALESCE(d.luminary_kills, 0) ELSE 0 END AS luminary_kills,
                    CASE WHEN d.uuid IS NOT NULL THEN c.luminary_deaths - COALESCE(d.luminary_deaths, 0) ELSE 0 END AS luminary_deaths
                FROM experimental_specs_wl c
                LEFT JOIN experimental_specs_wl_daily d
                    ON c.uuid = d.uuid
                    AND d.day_start_date = (SELECT MAX(day_start_date) FROM experimental_specs_wl_daily);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS experimental_specs_wl_current_day;");
        }
    }
}
