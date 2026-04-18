using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseWeightHistoryTablesAndViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "base_weights_daily",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    day_start_date = table.Column<int>(type: "integer", nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_base_weights_daily", x => new { x.uuid, x.day_start_date });
                });

            migrationBuilder.CreateTable(
                name: "base_weights_weekly",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    week_start_date = table.Column<int>(type: "integer", nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_base_weights_weekly", x => new { x.uuid, x.week_start_date });
                });

            migrationBuilder.Sql("""
                CREATE OR REPLACE VIEW base_weights_current_day AS
                SELECT
                    c.uuid,
                    c.weight AS current_weight,
                    d.weight AS previous_weight,
                    (SELECT id FROM time_day ORDER BY id DESC OFFSET 1 LIMIT 1) AS day_start_date,
                    CASE WHEN d.uuid IS NOT NULL THEN c.weight - d.weight ELSE 0 END AS weight_change
                FROM base_weights c
                LEFT JOIN base_weights_daily d
                    ON c.uuid = d.uuid
                    AND d.day_start_date = (SELECT id FROM time_day ORDER BY id DESC OFFSET 1 LIMIT 1);
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE VIEW base_weights_current_week AS
                SELECT
                    c.uuid,
                    c.weight AS current_weight,
                    d.weight AS previous_weight,
                    (SELECT id FROM time_week ORDER BY id DESC OFFSET 1 LIMIT 1) AS week_start_date,
                    CASE WHEN d.uuid IS NOT NULL THEN c.weight - d.weight ELSE 0 END AS weight_change
                FROM base_weights c
                LEFT JOIN base_weights_weekly d
                    ON c.uuid = d.uuid
                    AND d.week_start_date = (SELECT id FROM time_week ORDER BY id DESC OFFSET 1 LIMIT 1);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS base_weights_current_day;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS base_weights_current_week;");

            migrationBuilder.DropTable(
                name: "base_weights_daily");

            migrationBuilder.DropTable(
                name: "base_weights_weekly");
        }
    }
}
