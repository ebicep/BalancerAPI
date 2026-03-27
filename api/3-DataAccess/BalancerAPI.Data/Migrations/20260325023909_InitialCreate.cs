using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                    pyromancer = table.Column<int>(type: "integer", nullable: false),
                    cryomancer = table.Column<int>(type: "integer", nullable: false),
                    aquamancer = table.Column<int>(type: "integer", nullable: false),
                    berserker = table.Column<int>(type: "integer", nullable: false),
                    defender = table.Column<int>(type: "integer", nullable: false),
                    revenant = table.Column<int>(type: "integer", nullable: false),
                    avenger = table.Column<int>(type: "integer", nullable: false),
                    crusader = table.Column<int>(type: "integer", nullable: false),
                    protector = table.Column<int>(type: "integer", nullable: false),
                    thunderlord = table.Column<int>(type: "integer", nullable: false),
                    spiritguard = table.Column<int>(type: "integer", nullable: false),
                    earthwarden = table.Column<int>(type: "integer", nullable: false),
                    assassin = table.Column<int>(type: "integer", nullable: false),
                    vindicator = table.Column<int>(type: "integer", nullable: false),
                    apothecary = table.Column<int>(type: "integer", nullable: false),
                    conjurer = table.Column<int>(type: "integer", nullable: false),
                    sentinel = table.Column<int>(type: "integer", nullable: false),
                    luminary = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adjust_log_weekly", x => x.uuid);
                });

            migrationBuilder.CreateTable(
                name: "base_weights",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_base_weights", x => x.uuid);
                });

            migrationBuilder.CreateTable(
                name: "experimental_spec_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    pyromancer = table.Column<Guid>(type: "uuid", nullable: true),
                    cryomancer = table.Column<Guid>(type: "uuid", nullable: true),
                    aquamancer = table.Column<Guid>(type: "uuid", nullable: true),
                    berserker = table.Column<Guid>(type: "uuid", nullable: true),
                    defender = table.Column<Guid>(type: "uuid", nullable: true),
                    revenant = table.Column<Guid>(type: "uuid", nullable: true),
                    avenger = table.Column<Guid>(type: "uuid", nullable: true),
                    crusader = table.Column<Guid>(type: "uuid", nullable: true),
                    protector = table.Column<Guid>(type: "uuid", nullable: true),
                    thunderlord = table.Column<Guid>(type: "uuid", nullable: true),
                    spiritguard = table.Column<Guid>(type: "uuid", nullable: true),
                    earthwarden = table.Column<Guid>(type: "uuid", nullable: true),
                    assassin = table.Column<Guid>(type: "uuid", nullable: true),
                    vindicator = table.Column<Guid>(type: "uuid", nullable: true),
                    apothecary = table.Column<Guid>(type: "uuid", nullable: true),
                    conjurer = table.Column<Guid>(type: "uuid", nullable: true),
                    sentinel = table.Column<Guid>(type: "uuid", nullable: true),
                    luminary = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experimental_spec_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "experimental_spec_weights",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    pyromancer_offset = table.Column<int>(type: "integer", nullable: false),
                    cryomancer_offset = table.Column<int>(type: "integer", nullable: false),
                    aquamancer_offset = table.Column<int>(type: "integer", nullable: false),
                    berserker_offset = table.Column<int>(type: "integer", nullable: false),
                    defender_offset = table.Column<int>(type: "integer", nullable: false),
                    revenant_offset = table.Column<int>(type: "integer", nullable: false),
                    avenger_offset = table.Column<int>(type: "integer", nullable: false),
                    crusader_offset = table.Column<int>(type: "integer", nullable: false),
                    protector_offset = table.Column<int>(type: "integer", nullable: false),
                    thunderlord_offset = table.Column<int>(type: "integer", nullable: false),
                    spiritguard_offset = table.Column<int>(type: "integer", nullable: false),
                    earthwarden_offset = table.Column<int>(type: "integer", nullable: false),
                    assassin_offset = table.Column<int>(type: "integer", nullable: false),
                    vindicator_offset = table.Column<int>(type: "integer", nullable: false),
                    apothecary_offset = table.Column<int>(type: "integer", nullable: false),
                    conjurer_offset = table.Column<int>(type: "integer", nullable: false),
                    sentinel_offset = table.Column<int>(type: "integer", nullable: false),
                    luminary_offset = table.Column<int>(type: "integer", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experimental_spec_weights", x => x.uuid);
                });

            migrationBuilder.CreateTable(
                name: "experimental_spec_weights_weekly",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    week_start_date = table.Column<int>(type: "integer", nullable: false),
                    pyromancer_offset = table.Column<int>(type: "integer", nullable: false),
                    cryomancer_offset = table.Column<int>(type: "integer", nullable: false),
                    aquamancer_offset = table.Column<int>(type: "integer", nullable: false),
                    berserker_offset = table.Column<int>(type: "integer", nullable: false),
                    defender_offset = table.Column<int>(type: "integer", nullable: false),
                    revenant_offset = table.Column<int>(type: "integer", nullable: false),
                    avenger_offset = table.Column<int>(type: "integer", nullable: false),
                    crusader_offset = table.Column<int>(type: "integer", nullable: false),
                    protector_offset = table.Column<int>(type: "integer", nullable: false),
                    thunderlord_offset = table.Column<int>(type: "integer", nullable: false),
                    spiritguard_offset = table.Column<int>(type: "integer", nullable: false),
                    earthwarden_offset = table.Column<int>(type: "integer", nullable: false),
                    assassin_offset = table.Column<int>(type: "integer", nullable: false),
                    vindicator_offset = table.Column<int>(type: "integer", nullable: false),
                    apothecary_offset = table.Column<int>(type: "integer", nullable: false),
                    conjurer_offset = table.Column<int>(type: "integer", nullable: false),
                    sentinel_offset = table.Column<int>(type: "integer", nullable: false),
                    luminary_offset = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experimental_spec_weights_weekly", x => new { x.uuid, x.week_start_date });
                });

            migrationBuilder.CreateTable(
                name: "experimental_specs_wl",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_experimental_specs_wl", x => x.uuid);
                });

            migrationBuilder.CreateTable(
                name: "experimental_specs_wl_daily",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    day_start_date = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_experimental_specs_wl_daily", x => new { x.uuid, x.day_start_date });
                });

            migrationBuilder.CreateTable(
                name: "experimental_specs_wl_weekly",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    week_start_date = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_experimental_specs_wl_weekly", x => new { x.uuid, x.week_start_date });
                });

            migrationBuilder.CreateTable(
                name: "names",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    previous_names = table.Column<string[]>(type: "character varying(16)[]", nullable: false, defaultValueSql: "'{}'::character varying[]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_names", x => x.uuid);
                });

            migrationBuilder.CreateTable(
                name: "time_day",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_day", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "time_week",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_week", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "time_season",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_season", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adjust_log_daily");

            migrationBuilder.DropTable(
                name: "adjust_log_weekly");

            migrationBuilder.DropTable(
                name: "base_weights");

            migrationBuilder.DropTable(
                name: "experimental_spec_logs");

            migrationBuilder.DropTable(
                name: "experimental_spec_weights");

            migrationBuilder.DropTable(
                name: "experimental_spec_weights_weekly");

            migrationBuilder.DropTable(
                name: "experimental_specs_wl");

            migrationBuilder.DropTable(
                name: "experimental_specs_wl_daily");

            migrationBuilder.DropTable(
                name: "experimental_specs_wl_weekly");

            migrationBuilder.DropTable(
                name: "names");

            migrationBuilder.DropTable(
                name: "time_day");

            migrationBuilder.DropTable(
                name: "time_week");

            migrationBuilder.DropTable(
                name: "time_season");
        }
    }
}
