using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddExperimentalSpecsWlUncount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "uncount",
                table: "experimental_balance_log",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "experimental_specs_wl_uncount",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
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
                    table.PrimaryKey("PK_experimental_specs_wl_uncount", x => x.uuid);
                });

            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_experimental_specs_wl_uncount_last_updated ON experimental_specs_wl_uncount;
                CREATE TRIGGER trg_experimental_specs_wl_uncount_last_updated
                    BEFORE INSERT OR UPDATE ON experimental_specs_wl_uncount
                    FOR EACH ROW
                    EXECUTE FUNCTION set_last_updated();
                """);

            migrationBuilder.Sql("""
                CREATE VIEW experimental_specs_wl_total AS
                SELECT
                    c.uuid,
                    c.pyromancer_wins + COALESCE(u.pyromancer_wins, 0) AS pyromancer_wins,
                    c.pyromancer_losses + COALESCE(u.pyromancer_losses, 0) AS pyromancer_losses,
                    c.pyromancer_kills + COALESCE(u.pyromancer_kills, 0) AS pyromancer_kills,
                    c.pyromancer_deaths + COALESCE(u.pyromancer_deaths, 0) AS pyromancer_deaths,
                    c.cryomancer_wins + COALESCE(u.cryomancer_wins, 0) AS cryomancer_wins,
                    c.cryomancer_losses + COALESCE(u.cryomancer_losses, 0) AS cryomancer_losses,
                    c.cryomancer_kills + COALESCE(u.cryomancer_kills, 0) AS cryomancer_kills,
                    c.cryomancer_deaths + COALESCE(u.cryomancer_deaths, 0) AS cryomancer_deaths,
                    c.aquamancer_wins + COALESCE(u.aquamancer_wins, 0) AS aquamancer_wins,
                    c.aquamancer_losses + COALESCE(u.aquamancer_losses, 0) AS aquamancer_losses,
                    c.aquamancer_kills + COALESCE(u.aquamancer_kills, 0) AS aquamancer_kills,
                    c.aquamancer_deaths + COALESCE(u.aquamancer_deaths, 0) AS aquamancer_deaths,
                    c.berserker_wins + COALESCE(u.berserker_wins, 0) AS berserker_wins,
                    c.berserker_losses + COALESCE(u.berserker_losses, 0) AS berserker_losses,
                    c.berserker_kills + COALESCE(u.berserker_kills, 0) AS berserker_kills,
                    c.berserker_deaths + COALESCE(u.berserker_deaths, 0) AS berserker_deaths,
                    c.defender_wins + COALESCE(u.defender_wins, 0) AS defender_wins,
                    c.defender_losses + COALESCE(u.defender_losses, 0) AS defender_losses,
                    c.defender_kills + COALESCE(u.defender_kills, 0) AS defender_kills,
                    c.defender_deaths + COALESCE(u.defender_deaths, 0) AS defender_deaths,
                    c.revenant_wins + COALESCE(u.revenant_wins, 0) AS revenant_wins,
                    c.revenant_losses + COALESCE(u.revenant_losses, 0) AS revenant_losses,
                    c.revenant_kills + COALESCE(u.revenant_kills, 0) AS revenant_kills,
                    c.revenant_deaths + COALESCE(u.revenant_deaths, 0) AS revenant_deaths,
                    c.avenger_wins + COALESCE(u.avenger_wins, 0) AS avenger_wins,
                    c.avenger_losses + COALESCE(u.avenger_losses, 0) AS avenger_losses,
                    c.avenger_kills + COALESCE(u.avenger_kills, 0) AS avenger_kills,
                    c.avenger_deaths + COALESCE(u.avenger_deaths, 0) AS avenger_deaths,
                    c.crusader_wins + COALESCE(u.crusader_wins, 0) AS crusader_wins,
                    c.crusader_losses + COALESCE(u.crusader_losses, 0) AS crusader_losses,
                    c.crusader_kills + COALESCE(u.crusader_kills, 0) AS crusader_kills,
                    c.crusader_deaths + COALESCE(u.crusader_deaths, 0) AS crusader_deaths,
                    c.protector_wins + COALESCE(u.protector_wins, 0) AS protector_wins,
                    c.protector_losses + COALESCE(u.protector_losses, 0) AS protector_losses,
                    c.protector_kills + COALESCE(u.protector_kills, 0) AS protector_kills,
                    c.protector_deaths + COALESCE(u.protector_deaths, 0) AS protector_deaths,
                    c.thunderlord_wins + COALESCE(u.thunderlord_wins, 0) AS thunderlord_wins,
                    c.thunderlord_losses + COALESCE(u.thunderlord_losses, 0) AS thunderlord_losses,
                    c.thunderlord_kills + COALESCE(u.thunderlord_kills, 0) AS thunderlord_kills,
                    c.thunderlord_deaths + COALESCE(u.thunderlord_deaths, 0) AS thunderlord_deaths,
                    c.spiritguard_wins + COALESCE(u.spiritguard_wins, 0) AS spiritguard_wins,
                    c.spiritguard_losses + COALESCE(u.spiritguard_losses, 0) AS spiritguard_losses,
                    c.spiritguard_kills + COALESCE(u.spiritguard_kills, 0) AS spiritguard_kills,
                    c.spiritguard_deaths + COALESCE(u.spiritguard_deaths, 0) AS spiritguard_deaths,
                    c.earthwarden_wins + COALESCE(u.earthwarden_wins, 0) AS earthwarden_wins,
                    c.earthwarden_losses + COALESCE(u.earthwarden_losses, 0) AS earthwarden_losses,
                    c.earthwarden_kills + COALESCE(u.earthwarden_kills, 0) AS earthwarden_kills,
                    c.earthwarden_deaths + COALESCE(u.earthwarden_deaths, 0) AS earthwarden_deaths,
                    c.assassin_wins + COALESCE(u.assassin_wins, 0) AS assassin_wins,
                    c.assassin_losses + COALESCE(u.assassin_losses, 0) AS assassin_losses,
                    c.assassin_kills + COALESCE(u.assassin_kills, 0) AS assassin_kills,
                    c.assassin_deaths + COALESCE(u.assassin_deaths, 0) AS assassin_deaths,
                    c.vindicator_wins + COALESCE(u.vindicator_wins, 0) AS vindicator_wins,
                    c.vindicator_losses + COALESCE(u.vindicator_losses, 0) AS vindicator_losses,
                    c.vindicator_kills + COALESCE(u.vindicator_kills, 0) AS vindicator_kills,
                    c.vindicator_deaths + COALESCE(u.vindicator_deaths, 0) AS vindicator_deaths,
                    c.apothecary_wins + COALESCE(u.apothecary_wins, 0) AS apothecary_wins,
                    c.apothecary_losses + COALESCE(u.apothecary_losses, 0) AS apothecary_losses,
                    c.apothecary_kills + COALESCE(u.apothecary_kills, 0) AS apothecary_kills,
                    c.apothecary_deaths + COALESCE(u.apothecary_deaths, 0) AS apothecary_deaths,
                    c.conjurer_wins + COALESCE(u.conjurer_wins, 0) AS conjurer_wins,
                    c.conjurer_losses + COALESCE(u.conjurer_losses, 0) AS conjurer_losses,
                    c.conjurer_kills + COALESCE(u.conjurer_kills, 0) AS conjurer_kills,
                    c.conjurer_deaths + COALESCE(u.conjurer_deaths, 0) AS conjurer_deaths,
                    c.sentinel_wins + COALESCE(u.sentinel_wins, 0) AS sentinel_wins,
                    c.sentinel_losses + COALESCE(u.sentinel_losses, 0) AS sentinel_losses,
                    c.sentinel_kills + COALESCE(u.sentinel_kills, 0) AS sentinel_kills,
                    c.sentinel_deaths + COALESCE(u.sentinel_deaths, 0) AS sentinel_deaths,
                    c.luminary_wins + COALESCE(u.luminary_wins, 0) AS luminary_wins,
                    c.luminary_losses + COALESCE(u.luminary_losses, 0) AS luminary_losses,
                    c.luminary_kills + COALESCE(u.luminary_kills, 0) AS luminary_kills,
                    c.luminary_deaths + COALESCE(u.luminary_deaths, 0) AS luminary_deaths,
                    GREATEST(c.last_updated, COALESCE(u.last_updated, c.last_updated)) AS last_updated
                FROM experimental_specs_wl c
                LEFT JOIN experimental_specs_wl_uncount u
                    ON c.uuid = u.uuid;
                """);

            RecreateCurrentWlViews(migrationBuilder, sourceTable: "experimental_specs_wl_total");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RecreateCurrentWlViews(migrationBuilder, sourceTable: "experimental_specs_wl");

            migrationBuilder.Sql("""
                DROP VIEW IF EXISTS experimental_specs_wl_total;
                DROP TRIGGER IF EXISTS trg_experimental_specs_wl_uncount_last_updated ON experimental_specs_wl_uncount;
                """);

            migrationBuilder.DropTable(
                name: "experimental_specs_wl_uncount");

            migrationBuilder.DropColumn(
                name: "uncount",
                table: "experimental_balance_log");
        }

        private static void RecreateCurrentWlViews(MigrationBuilder migrationBuilder, string sourceTable)
        {
            migrationBuilder.Sql($"""
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
                FROM {sourceTable} c
                LEFT JOIN experimental_specs_wl_weekly w
                    ON c.uuid = w.uuid
                    AND w.week_start_date = (SELECT id FROM time_week ORDER BY id DESC OFFSET 0 LIMIT 1);
                """);

            migrationBuilder.Sql($"""
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
                FROM {sourceTable} c
                LEFT JOIN experimental_specs_wl_daily d
                    ON c.uuid = d.uuid
                    AND d.day_start_date = (SELECT id FROM time_day ORDER BY id DESC OFFSET 0 LIMIT 1);
                """);
        }
    }
}
