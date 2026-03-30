using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddBalancingPlayerDataView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE VIEW experimental_balance_player_data AS
                SELECT
                    b.uuid,
                    n.name,
                    b.weight AS base_weight,
                    b.weight + s.pyromancer_offset AS pyromancer_weight,
                    b.weight + s.cryomancer_offset AS cryomancer_weight,
                    b.weight + s.aquamancer_offset AS aquamancer_weight,
                    b.weight + s.berserker_offset AS berserker_weight,
                    b.weight + s.defender_offset AS defender_weight,
                    b.weight + s.revenant_offset AS revenant_weight,
                    b.weight + s.avenger_offset AS avenger_weight,
                    b.weight + s.crusader_offset AS crusader_weight,
                    b.weight + s.protector_offset AS protector_weight,
                    b.weight + s.thunderlord_offset AS thunderlord_weight,
                    b.weight + s.spiritguard_offset AS spiritguard_weight,
                    b.weight + s.earthwarden_offset AS earthwarden_weight,
                    b.weight + s.assassin_offset AS assassin_weight,
                    b.weight + s.vindicator_offset AS vindicator_weight,
                    b.weight + s.apothecary_offset AS apothecary_weight,
                    b.weight + s.conjurer_offset AS conjurer_weight,
                    b.weight + s.sentinel_offset AS sentinel_weight,
                    b.weight + s.luminary_offset AS luminary_weight,
                    COALESCE(w.pyromancer_wins, 0) AS pyromancer_wins,
                    COALESCE(w.pyromancer_losses, 0) AS pyromancer_losses,
                    COALESCE(w.pyromancer_kills, 0) AS pyromancer_kills,
                    COALESCE(w.pyromancer_deaths, 0) AS pyromancer_deaths,
                    COALESCE(w.cryomancer_wins, 0) AS cryomancer_wins,
                    COALESCE(w.cryomancer_losses, 0) AS cryomancer_losses,
                    COALESCE(w.cryomancer_kills, 0) AS cryomancer_kills,
                    COALESCE(w.cryomancer_deaths, 0) AS cryomancer_deaths,
                    COALESCE(w.aquamancer_wins, 0) AS aquamancer_wins,
                    COALESCE(w.aquamancer_losses, 0) AS aquamancer_losses,
                    COALESCE(w.aquamancer_kills, 0) AS aquamancer_kills,
                    COALESCE(w.aquamancer_deaths, 0) AS aquamancer_deaths,
                    COALESCE(w.berserker_wins, 0) AS berserker_wins,
                    COALESCE(w.berserker_losses, 0) AS berserker_losses,
                    COALESCE(w.berserker_kills, 0) AS berserker_kills,
                    COALESCE(w.berserker_deaths, 0) AS berserker_deaths,
                    COALESCE(w.defender_wins, 0) AS defender_wins,
                    COALESCE(w.defender_losses, 0) AS defender_losses,
                    COALESCE(w.defender_kills, 0) AS defender_kills,
                    COALESCE(w.defender_deaths, 0) AS defender_deaths,
                    COALESCE(w.revenant_wins, 0) AS revenant_wins,
                    COALESCE(w.revenant_losses, 0) AS revenant_losses,
                    COALESCE(w.revenant_kills, 0) AS revenant_kills,
                    COALESCE(w.revenant_deaths, 0) AS revenant_deaths,
                    COALESCE(w.avenger_wins, 0) AS avenger_wins,
                    COALESCE(w.avenger_losses, 0) AS avenger_losses,
                    COALESCE(w.avenger_kills, 0) AS avenger_kills,
                    COALESCE(w.avenger_deaths, 0) AS avenger_deaths,
                    COALESCE(w.crusader_wins, 0) AS crusader_wins,
                    COALESCE(w.crusader_losses, 0) AS crusader_losses,
                    COALESCE(w.crusader_kills, 0) AS crusader_kills,
                    COALESCE(w.crusader_deaths, 0) AS crusader_deaths,
                    COALESCE(w.protector_wins, 0) AS protector_wins,
                    COALESCE(w.protector_losses, 0) AS protector_losses,
                    COALESCE(w.protector_kills, 0) AS protector_kills,
                    COALESCE(w.protector_deaths, 0) AS protector_deaths,
                    COALESCE(w.thunderlord_wins, 0) AS thunderlord_wins,
                    COALESCE(w.thunderlord_losses, 0) AS thunderlord_losses,
                    COALESCE(w.thunderlord_kills, 0) AS thunderlord_kills,
                    COALESCE(w.thunderlord_deaths, 0) AS thunderlord_deaths,
                    COALESCE(w.spiritguard_wins, 0) AS spiritguard_wins,
                    COALESCE(w.spiritguard_losses, 0) AS spiritguard_losses,
                    COALESCE(w.spiritguard_kills, 0) AS spiritguard_kills,
                    COALESCE(w.spiritguard_deaths, 0) AS spiritguard_deaths,
                    COALESCE(w.earthwarden_wins, 0) AS earthwarden_wins,
                    COALESCE(w.earthwarden_losses, 0) AS earthwarden_losses,
                    COALESCE(w.earthwarden_kills, 0) AS earthwarden_kills,
                    COALESCE(w.earthwarden_deaths, 0) AS earthwarden_deaths,
                    COALESCE(w.assassin_wins, 0) AS assassin_wins,
                    COALESCE(w.assassin_losses, 0) AS assassin_losses,
                    COALESCE(w.assassin_kills, 0) AS assassin_kills,
                    COALESCE(w.assassin_deaths, 0) AS assassin_deaths,
                    COALESCE(w.vindicator_wins, 0) AS vindicator_wins,
                    COALESCE(w.vindicator_losses, 0) AS vindicator_losses,
                    COALESCE(w.vindicator_kills, 0) AS vindicator_kills,
                    COALESCE(w.vindicator_deaths, 0) AS vindicator_deaths,
                    COALESCE(w.apothecary_wins, 0) AS apothecary_wins,
                    COALESCE(w.apothecary_losses, 0) AS apothecary_losses,
                    COALESCE(w.apothecary_kills, 0) AS apothecary_kills,
                    COALESCE(w.apothecary_deaths, 0) AS apothecary_deaths,
                    COALESCE(w.conjurer_wins, 0) AS conjurer_wins,
                    COALESCE(w.conjurer_losses, 0) AS conjurer_losses,
                    COALESCE(w.conjurer_kills, 0) AS conjurer_kills,
                    COALESCE(w.conjurer_deaths, 0) AS conjurer_deaths,
                    COALESCE(w.sentinel_wins, 0) AS sentinel_wins,
                    COALESCE(w.sentinel_losses, 0) AS sentinel_losses,
                    COALESCE(w.sentinel_kills, 0) AS sentinel_kills,
                    COALESCE(w.sentinel_deaths, 0) AS sentinel_deaths,
                    COALESCE(w.luminary_wins, 0) AS luminary_wins,
                    COALESCE(w.luminary_losses, 0) AS luminary_losses,
                    COALESCE(w.luminary_kills, 0) AS luminary_kills,
                    COALESCE(w.luminary_deaths, 0) AS luminary_deaths,
                    (
                        COALESCE(d.pyromancer_wins, 0) - COALESCE(d.pyromancer_losses, 0) +
                        COALESCE(d.cryomancer_wins, 0) - COALESCE(d.cryomancer_losses, 0) +
                        COALESCE(d.aquamancer_wins, 0) - COALESCE(d.aquamancer_losses, 0) +
                        COALESCE(d.berserker_wins, 0) - COALESCE(d.berserker_losses, 0) +
                        COALESCE(d.defender_wins, 0) - COALESCE(d.defender_losses, 0) +
                        COALESCE(d.revenant_wins, 0) - COALESCE(d.revenant_losses, 0) +
                        COALESCE(d.avenger_wins, 0) - COALESCE(d.avenger_losses, 0) +
                        COALESCE(d.crusader_wins, 0) - COALESCE(d.crusader_losses, 0) +
                        COALESCE(d.protector_wins, 0) - COALESCE(d.protector_losses, 0) +
                        COALESCE(d.thunderlord_wins, 0) - COALESCE(d.thunderlord_losses, 0) +
                        COALESCE(d.spiritguard_wins, 0) - COALESCE(d.spiritguard_losses, 0) +
                        COALESCE(d.earthwarden_wins, 0) - COALESCE(d.earthwarden_losses, 0) +
                        COALESCE(d.assassin_wins, 0) - COALESCE(d.assassin_losses, 0) +
                        COALESCE(d.vindicator_wins, 0) - COALESCE(d.vindicator_losses, 0) +
                        COALESCE(d.apothecary_wins, 0) - COALESCE(d.apothecary_losses, 0) +
                        COALESCE(d.conjurer_wins, 0) - COALESCE(d.conjurer_losses, 0) +
                        COALESCE(d.sentinel_wins, 0) - COALESCE(d.sentinel_losses, 0) +
                        COALESCE(d.luminary_wins, 0) - COALESCE(d.luminary_losses, 0)
                    ) AS daily_win_loss,
                    (
                        ROUND(
                        (
                        CASE
                            WHEN (
                                COALESCE(w.pyromancer_wins, 0) + COALESCE(w.cryomancer_wins, 0) +
                                COALESCE(w.aquamancer_wins, 0) + COALESCE(w.berserker_wins, 0) +
                                COALESCE(w.defender_wins, 0) + COALESCE(w.revenant_wins, 0) +
                                COALESCE(w.avenger_wins, 0) + COALESCE(w.crusader_wins, 0) +
                                COALESCE(w.protector_wins, 0) + COALESCE(w.thunderlord_wins, 0) +
                                COALESCE(w.spiritguard_wins, 0) + COALESCE(w.earthwarden_wins, 0) +
                                COALESCE(w.assassin_wins, 0) + COALESCE(w.vindicator_wins, 0) +
                                COALESCE(w.apothecary_wins, 0) + COALESCE(w.conjurer_wins, 0) +
                                COALESCE(w.sentinel_wins, 0) + COALESCE(w.luminary_wins, 0) +
                                COALESCE(w.pyromancer_losses, 0) + COALESCE(w.cryomancer_losses, 0) +
                                COALESCE(w.aquamancer_losses, 0) + COALESCE(w.berserker_losses, 0) +
                                COALESCE(w.defender_losses, 0) + COALESCE(w.revenant_losses, 0) +
                                COALESCE(w.avenger_losses, 0) + COALESCE(w.crusader_losses, 0) +
                                COALESCE(w.protector_losses, 0) + COALESCE(w.thunderlord_losses, 0) +
                                COALESCE(w.spiritguard_losses, 0) + COALESCE(w.earthwarden_losses, 0) +
                                COALESCE(w.assassin_losses, 0) + COALESCE(w.vindicator_losses, 0) +
                                COALESCE(w.apothecary_losses, 0) + COALESCE(w.conjurer_losses, 0) +
                                COALESCE(w.sentinel_losses, 0) + COALESCE(w.luminary_losses, 0)
                            ) = 0 THEN 0::double precision
                            ELSE (
                                COALESCE(w.pyromancer_kills, 0) - COALESCE(w.pyromancer_deaths, 0) +
                                COALESCE(w.cryomancer_kills, 0) - COALESCE(w.cryomancer_deaths, 0) +
                                COALESCE(w.aquamancer_kills, 0) - COALESCE(w.aquamancer_deaths, 0) +
                                COALESCE(w.berserker_kills, 0) - COALESCE(w.berserker_deaths, 0) +
                                COALESCE(w.defender_kills, 0) - COALESCE(w.defender_deaths, 0) +
                                COALESCE(w.revenant_kills, 0) - COALESCE(w.revenant_deaths, 0) +
                                COALESCE(w.avenger_kills, 0) - COALESCE(w.avenger_deaths, 0) +
                                COALESCE(w.crusader_kills, 0) - COALESCE(w.crusader_deaths, 0) +
                                COALESCE(w.protector_kills, 0) - COALESCE(w.protector_deaths, 0) +
                                COALESCE(w.thunderlord_kills, 0) - COALESCE(w.thunderlord_deaths, 0) +
                                COALESCE(w.spiritguard_kills, 0) - COALESCE(w.spiritguard_deaths, 0) +
                                COALESCE(w.earthwarden_kills, 0) - COALESCE(w.earthwarden_deaths, 0) +
                                COALESCE(w.assassin_kills, 0) - COALESCE(w.assassin_deaths, 0) +
                                COALESCE(w.vindicator_kills, 0) - COALESCE(w.vindicator_deaths, 0) +
                                COALESCE(w.apothecary_kills, 0) - COALESCE(w.apothecary_deaths, 0) +
                                COALESCE(w.conjurer_kills, 0) - COALESCE(w.conjurer_deaths, 0) +
                                COALESCE(w.sentinel_kills, 0) - COALESCE(w.sentinel_deaths, 0) +
                                COALESCE(w.luminary_kills, 0) - COALESCE(w.luminary_deaths, 0)
                            )::double precision / (
                                COALESCE(w.pyromancer_wins, 0) + COALESCE(w.cryomancer_wins, 0) +
                                COALESCE(w.aquamancer_wins, 0) + COALESCE(w.berserker_wins, 0) +
                                COALESCE(w.defender_wins, 0) + COALESCE(w.revenant_wins, 0) +
                                COALESCE(w.avenger_wins, 0) + COALESCE(w.crusader_wins, 0) +
                                COALESCE(w.protector_wins, 0) + COALESCE(w.thunderlord_wins, 0) +
                                COALESCE(w.spiritguard_wins, 0) + COALESCE(w.earthwarden_wins, 0) +
                                COALESCE(w.assassin_wins, 0) + COALESCE(w.vindicator_wins, 0) +
                                COALESCE(w.apothecary_wins, 0) + COALESCE(w.conjurer_wins, 0) +
                                COALESCE(w.sentinel_wins, 0) + COALESCE(w.luminary_wins, 0) +
                                COALESCE(w.pyromancer_losses, 0) + COALESCE(w.cryomancer_losses, 0) +
                                COALESCE(w.aquamancer_losses, 0) + COALESCE(w.berserker_losses, 0) +
                                COALESCE(w.defender_losses, 0) + COALESCE(w.revenant_losses, 0) +
                                COALESCE(w.avenger_losses, 0) + COALESCE(w.crusader_losses, 0) +
                                COALESCE(w.protector_losses, 0) + COALESCE(w.thunderlord_losses, 0) +
                                COALESCE(w.spiritguard_losses, 0) + COALESCE(w.earthwarden_losses, 0) +
                                COALESCE(w.assassin_losses, 0) + COALESCE(w.vindicator_losses, 0) +
                                COALESCE(w.apothecary_losses, 0) + COALESCE(w.conjurer_losses, 0) +
                                COALESCE(w.sentinel_losses, 0) + COALESCE(w.luminary_losses, 0)
                            )::double precision
                        END
                        )::numeric,
                        2
                    )::double precision
                    ) AS global_net_kd_per_game
                FROM base_weights b
                JOIN experimental_spec_weights s ON b.uuid = s.uuid
                LEFT JOIN experimental_specs_wl w ON b.uuid = w.uuid
                LEFT JOIN experimental_specs_wl_current_day d ON b.uuid = d.uuid
                LEFT JOIN names n ON b.uuid = n.uuid;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS experimental_balance_player_data;");
        }
    }
}
