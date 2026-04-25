using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseWeightLastPlayed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_played",
                table: "base_weights",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_base_weights_last_updated ON base_weights;

                CREATE TRIGGER trg_base_weights_last_updated_insert
                    BEFORE INSERT ON base_weights
                    FOR EACH ROW
                    EXECUTE FUNCTION set_last_updated();

                CREATE TRIGGER trg_base_weights_last_updated_update
                    BEFORE UPDATE ON base_weights
                    FOR EACH ROW
                    WHEN (OLD.weight IS DISTINCT FROM NEW.weight)
                    EXECUTE FUNCTION set_last_updated();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_base_weights_last_updated_update ON base_weights;
                DROP TRIGGER IF EXISTS trg_base_weights_last_updated_insert ON base_weights;

                CREATE TRIGGER trg_base_weights_last_updated
                    BEFORE INSERT OR UPDATE ON base_weights
                    FOR EACH ROW
                    EXECUTE FUNCTION set_last_updated();
                """);

            migrationBuilder.DropColumn(
                name: "last_played",
                table: "base_weights");
        }
    }
}
