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
                    c.pyromancer_wins - COALESCE(w.pyromancer_wins, 0) AS pyromancer_wins,
                    c.pyromancer_losses - COALESCE(w.pyromancer_losses, 0) AS pyromancer_losses,
                    c.pyromancer_kills - COALESCE(w.pyromancer_kills, 0) AS pyromancer_kills,
                    c.pyromancer_deaths - COALESCE(w.pyromancer_deaths, 0) AS pyromancer_deaths,
                    c.cryomancer_wins - COALESCE(w.cryomancer_wins, 0) AS cryomancer_wins,
                    c.cryomancer_losses - COALESCE(w.cryomancer_losses, 0) AS cryomancer_losses,
                    c.cryomancer_kills - COALESCE(w.cryomancer_kills, 0) AS cryomancer_kills,
                    c.cryomancer_deaths - COALESCE(w.cryomancer_deaths, 0) AS cryomancer_deaths,
                    c.aquamancer_wins - COALESCE(w.aquamancer_wins, 0) AS aquamancer_wins,
                    c.aquamancer_losses - COALESCE(w.aquamancer_losses, 0) AS aquamancer_losses,
                    c.aquamancer_kills - COALESCE(w.aquamancer_kills, 0) AS aquamancer_kills,
                    c.aquamancer_deaths - COALESCE(w.aquamancer_deaths, 0) AS aquamancer_deaths,
                    c.berserker_wins - COALESCE(w.berserker_wins, 0) AS berserker_wins,
                    c.berserker_losses - COALESCE(w.berserker_losses, 0) AS berserker_losses,
                    c.berserker_kills - COALESCE(w.berserker_kills, 0) AS berserker_kills,
                    c.berserker_deaths - COALESCE(w.berserker_deaths, 0) AS berserker_deaths,
                    c.defender_wins - COALESCE(w.defender_wins, 0) AS defender_wins,
                    c.defender_losses - COALESCE(w.defender_losses, 0) AS defender_losses,
                    c.defender_kills - COALESCE(w.defender_kills, 0) AS defender_kills,
                    c.defender_deaths - COALESCE(w.defender_deaths, 0) AS defender_deaths,
                    c.revenant_wins - COALESCE(w.revenant_wins, 0) AS revenant_wins,
                    c.revenant_losses - COALESCE(w.revenant_losses, 0) AS revenant_losses,
                    c.revenant_kills - COALESCE(w.revenant_kills, 0) AS revenant_kills,
                    c.revenant_deaths - COALESCE(w.revenant_deaths, 0) AS revenant_deaths,
                    c.avenger_wins - COALESCE(w.avenger_wins, 0) AS avenger_wins,
                    c.avenger_losses - COALESCE(w.avenger_losses, 0) AS avenger_losses,
                    c.avenger_kills - COALESCE(w.avenger_kills, 0) AS avenger_kills,
                    c.avenger_deaths - COALESCE(w.avenger_deaths, 0) AS avenger_deaths,
                    c.crusader_wins - COALESCE(w.crusader_wins, 0) AS crusader_wins,
                    c.crusader_losses - COALESCE(w.crusader_losses, 0) AS crusader_losses,
                    c.crusader_kills - COALESCE(w.crusader_kills, 0) AS crusader_kills,
                    c.crusader_deaths - COALESCE(w.crusader_deaths, 0) AS crusader_deaths,
                    c.protector_wins - COALESCE(w.protector_wins, 0) AS protector_wins,
                    c.protector_losses - COALESCE(w.protector_losses, 0) AS protector_losses,
                    c.protector_kills - COALESCE(w.protector_kills, 0) AS protector_kills,
                    c.protector_deaths - COALESCE(w.protector_deaths, 0) AS protector_deaths,
                    c.thunderlord_wins - COALESCE(w.thunderlord_wins, 0) AS thunderlord_wins,
                    c.thunderlord_losses - COALESCE(w.thunderlord_losses, 0) AS thunderlord_losses,
                    c.thunderlord_kills - COALESCE(w.thunderlord_kills, 0) AS thunderlord_kills,
                    c.thunderlord_deaths - COALESCE(w.thunderlord_deaths, 0) AS thunderlord_deaths,
                    c.spiritguard_wins - COALESCE(w.spiritguard_wins, 0) AS spiritguard_wins,
                    c.spiritguard_losses - COALESCE(w.spiritguard_losses, 0) AS spiritguard_losses,
                    c.spiritguard_kills - COALESCE(w.spiritguard_kills, 0) AS spiritguard_kills,
                    c.spiritguard_deaths - COALESCE(w.spiritguard_deaths, 0) AS spiritguard_deaths,
                    c.earthwarden_wins - COALESCE(w.earthwarden_wins, 0) AS earthwarden_wins,
                    c.earthwarden_losses - COALESCE(w.earthwarden_losses, 0) AS earthwarden_losses,
                    c.earthwarden_kills - COALESCE(w.earthwarden_kills, 0) AS earthwarden_kills,
                    c.earthwarden_deaths - COALESCE(w.earthwarden_deaths, 0) AS earthwarden_deaths,
                    c.assassin_wins - COALESCE(w.assassin_wins, 0) AS assassin_wins,
                    c.assassin_losses - COALESCE(w.assassin_losses, 0) AS assassin_losses,
                    c.assassin_kills - COALESCE(w.assassin_kills, 0) AS assassin_kills,
                    c.assassin_deaths - COALESCE(w.assassin_deaths, 0) AS assassin_deaths,
                    c.vindicator_wins - COALESCE(w.vindicator_wins, 0) AS vindicator_wins,
                    c.vindicator_losses - COALESCE(w.vindicator_losses, 0) AS vindicator_losses,
                    c.vindicator_kills - COALESCE(w.vindicator_kills, 0) AS vindicator_kills,
                    c.vindicator_deaths - COALESCE(w.vindicator_deaths, 0) AS vindicator_deaths,
                    c.apothecary_wins - COALESCE(w.apothecary_wins, 0) AS apothecary_wins,
                    c.apothecary_losses - COALESCE(w.apothecary_losses, 0) AS apothecary_losses,
                    c.apothecary_kills - COALESCE(w.apothecary_kills, 0) AS apothecary_kills,
                    c.apothecary_deaths - COALESCE(w.apothecary_deaths, 0) AS apothecary_deaths,
                    c.conjurer_wins - COALESCE(w.conjurer_wins, 0) AS conjurer_wins,
                    c.conjurer_losses - COALESCE(w.conjurer_losses, 0) AS conjurer_losses,
                    c.conjurer_kills - COALESCE(w.conjurer_kills, 0) AS conjurer_kills,
                    c.conjurer_deaths - COALESCE(w.conjurer_deaths, 0) AS conjurer_deaths,
                    c.sentinel_wins - COALESCE(w.sentinel_wins, 0) AS sentinel_wins,
                    c.sentinel_losses - COALESCE(w.sentinel_losses, 0) AS sentinel_losses,
                    c.sentinel_kills - COALESCE(w.sentinel_kills, 0) AS sentinel_kills,
                    c.sentinel_deaths - COALESCE(w.sentinel_deaths, 0) AS sentinel_deaths,
                    c.luminary_wins - COALESCE(w.luminary_wins, 0) AS luminary_wins,
                    c.luminary_losses - COALESCE(w.luminary_losses, 0) AS luminary_losses,
                    c.luminary_kills - COALESCE(w.luminary_kills, 0) AS luminary_kills,
                    c.luminary_deaths - COALESCE(w.luminary_deaths, 0) AS luminary_deaths
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
