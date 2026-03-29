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
                    c.pyromancer_wins - COALESCE(d.pyromancer_wins, 0) AS pyromancer_wins,
                    c.pyromancer_losses - COALESCE(d.pyromancer_losses, 0) AS pyromancer_losses,
                    c.pyromancer_kills - COALESCE(d.pyromancer_kills, 0) AS pyromancer_kills,
                    c.pyromancer_deaths - COALESCE(d.pyromancer_deaths, 0) AS pyromancer_deaths,
                    c.cryomancer_wins - COALESCE(d.cryomancer_wins, 0) AS cryomancer_wins,
                    c.cryomancer_losses - COALESCE(d.cryomancer_losses, 0) AS cryomancer_losses,
                    c.cryomancer_kills - COALESCE(d.cryomancer_kills, 0) AS cryomancer_kills,
                    c.cryomancer_deaths - COALESCE(d.cryomancer_deaths, 0) AS cryomancer_deaths,
                    c.aquamancer_wins - COALESCE(d.aquamancer_wins, 0) AS aquamancer_wins,
                    c.aquamancer_losses - COALESCE(d.aquamancer_losses, 0) AS aquamancer_losses,
                    c.aquamancer_kills - COALESCE(d.aquamancer_kills, 0) AS aquamancer_kills,
                    c.aquamancer_deaths - COALESCE(d.aquamancer_deaths, 0) AS aquamancer_deaths,
                    c.berserker_wins - COALESCE(d.berserker_wins, 0) AS berserker_wins,
                    c.berserker_losses - COALESCE(d.berserker_losses, 0) AS berserker_losses,
                    c.berserker_kills - COALESCE(d.berserker_kills, 0) AS berserker_kills,
                    c.berserker_deaths - COALESCE(d.berserker_deaths, 0) AS berserker_deaths,
                    c.defender_wins - COALESCE(d.defender_wins, 0) AS defender_wins,
                    c.defender_losses - COALESCE(d.defender_losses, 0) AS defender_losses,
                    c.defender_kills - COALESCE(d.defender_kills, 0) AS defender_kills,
                    c.defender_deaths - COALESCE(d.defender_deaths, 0) AS defender_deaths,
                    c.revenant_wins - COALESCE(d.revenant_wins, 0) AS revenant_wins,
                    c.revenant_losses - COALESCE(d.revenant_losses, 0) AS revenant_losses,
                    c.revenant_kills - COALESCE(d.revenant_kills, 0) AS revenant_kills,
                    c.revenant_deaths - COALESCE(d.revenant_deaths, 0) AS revenant_deaths,
                    c.avenger_wins - COALESCE(d.avenger_wins, 0) AS avenger_wins,
                    c.avenger_losses - COALESCE(d.avenger_losses, 0) AS avenger_losses,
                    c.avenger_kills - COALESCE(d.avenger_kills, 0) AS avenger_kills,
                    c.avenger_deaths - COALESCE(d.avenger_deaths, 0) AS avenger_deaths,
                    c.crusader_wins - COALESCE(d.crusader_wins, 0) AS crusader_wins,
                    c.crusader_losses - COALESCE(d.crusader_losses, 0) AS crusader_losses,
                    c.crusader_kills - COALESCE(d.crusader_kills, 0) AS crusader_kills,
                    c.crusader_deaths - COALESCE(d.crusader_deaths, 0) AS crusader_deaths,
                    c.protector_wins - COALESCE(d.protector_wins, 0) AS protector_wins,
                    c.protector_losses - COALESCE(d.protector_losses, 0) AS protector_losses,
                    c.protector_kills - COALESCE(d.protector_kills, 0) AS protector_kills,
                    c.protector_deaths - COALESCE(d.protector_deaths, 0) AS protector_deaths,
                    c.thunderlord_wins - COALESCE(d.thunderlord_wins, 0) AS thunderlord_wins,
                    c.thunderlord_losses - COALESCE(d.thunderlord_losses, 0) AS thunderlord_losses,
                    c.thunderlord_kills - COALESCE(d.thunderlord_kills, 0) AS thunderlord_kills,
                    c.thunderlord_deaths - COALESCE(d.thunderlord_deaths, 0) AS thunderlord_deaths,
                    c.spiritguard_wins - COALESCE(d.spiritguard_wins, 0) AS spiritguard_wins,
                    c.spiritguard_losses - COALESCE(d.spiritguard_losses, 0) AS spiritguard_losses,
                    c.spiritguard_kills - COALESCE(d.spiritguard_kills, 0) AS spiritguard_kills,
                    c.spiritguard_deaths - COALESCE(d.spiritguard_deaths, 0) AS spiritguard_deaths,
                    c.earthwarden_wins - COALESCE(d.earthwarden_wins, 0) AS earthwarden_wins,
                    c.earthwarden_losses - COALESCE(d.earthwarden_losses, 0) AS earthwarden_losses,
                    c.earthwarden_kills - COALESCE(d.earthwarden_kills, 0) AS earthwarden_kills,
                    c.earthwarden_deaths - COALESCE(d.earthwarden_deaths, 0) AS earthwarden_deaths,
                    c.assassin_wins - COALESCE(d.assassin_wins, 0) AS assassin_wins,
                    c.assassin_losses - COALESCE(d.assassin_losses, 0) AS assassin_losses,
                    c.assassin_kills - COALESCE(d.assassin_kills, 0) AS assassin_kills,
                    c.assassin_deaths - COALESCE(d.assassin_deaths, 0) AS assassin_deaths,
                    c.vindicator_wins - COALESCE(d.vindicator_wins, 0) AS vindicator_wins,
                    c.vindicator_losses - COALESCE(d.vindicator_losses, 0) AS vindicator_losses,
                    c.vindicator_kills - COALESCE(d.vindicator_kills, 0) AS vindicator_kills,
                    c.vindicator_deaths - COALESCE(d.vindicator_deaths, 0) AS vindicator_deaths,
                    c.apothecary_wins - COALESCE(d.apothecary_wins, 0) AS apothecary_wins,
                    c.apothecary_losses - COALESCE(d.apothecary_losses, 0) AS apothecary_losses,
                    c.apothecary_kills - COALESCE(d.apothecary_kills, 0) AS apothecary_kills,
                    c.apothecary_deaths - COALESCE(d.apothecary_deaths, 0) AS apothecary_deaths,
                    c.conjurer_wins - COALESCE(d.conjurer_wins, 0) AS conjurer_wins,
                    c.conjurer_losses - COALESCE(d.conjurer_losses, 0) AS conjurer_losses,
                    c.conjurer_kills - COALESCE(d.conjurer_kills, 0) AS conjurer_kills,
                    c.conjurer_deaths - COALESCE(d.conjurer_deaths, 0) AS conjurer_deaths,
                    c.sentinel_wins - COALESCE(d.sentinel_wins, 0) AS sentinel_wins,
                    c.sentinel_losses - COALESCE(d.sentinel_losses, 0) AS sentinel_losses,
                    c.sentinel_kills - COALESCE(d.sentinel_kills, 0) AS sentinel_kills,
                    c.sentinel_deaths - COALESCE(d.sentinel_deaths, 0) AS sentinel_deaths,
                    c.luminary_wins - COALESCE(d.luminary_wins, 0) AS luminary_wins,
                    c.luminary_losses - COALESCE(d.luminary_losses, 0) AS luminary_losses,
                    c.luminary_kills - COALESCE(d.luminary_kills, 0) AS luminary_kills,
                    c.luminary_deaths - COALESCE(d.luminary_deaths, 0) AS luminary_deaths
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
