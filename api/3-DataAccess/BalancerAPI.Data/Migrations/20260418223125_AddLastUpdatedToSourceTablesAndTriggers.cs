using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLastUpdatedToSourceTablesAndTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_updated",
                table: "experimental_specs_wl",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_updated",
                table: "base_weights",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.Sql("""
                ALTER TABLE experimental_spec_weights
                    ALTER COLUMN last_updated SET DEFAULT now();

                CREATE OR REPLACE FUNCTION set_last_updated()
                RETURNS trigger AS $$
                BEGIN
                    NEW.last_updated = now();
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_base_weights_last_updated ON base_weights;
                CREATE TRIGGER trg_base_weights_last_updated
                    BEFORE INSERT OR UPDATE ON base_weights
                    FOR EACH ROW
                    EXECUTE FUNCTION set_last_updated();

                DROP TRIGGER IF EXISTS trg_experimental_specs_wl_last_updated ON experimental_specs_wl;
                CREATE TRIGGER trg_experimental_specs_wl_last_updated
                    BEFORE INSERT OR UPDATE ON experimental_specs_wl
                    FOR EACH ROW
                    EXECUTE FUNCTION set_last_updated();

                DROP TRIGGER IF EXISTS trg_experimental_spec_weights_last_updated ON experimental_spec_weights;
                CREATE TRIGGER trg_experimental_spec_weights_last_updated
                    BEFORE INSERT OR UPDATE ON experimental_spec_weights
                    FOR EACH ROW
                    EXECUTE FUNCTION set_last_updated();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_experimental_spec_weights_last_updated ON experimental_spec_weights;
                DROP TRIGGER IF EXISTS trg_experimental_specs_wl_last_updated ON experimental_specs_wl;
                DROP TRIGGER IF EXISTS trg_base_weights_last_updated ON base_weights;
                DROP FUNCTION IF EXISTS set_last_updated();

                ALTER TABLE experimental_spec_weights
                    ALTER COLUMN last_updated DROP DEFAULT;
                """);

            migrationBuilder.DropColumn(
                name: "last_updated",
                table: "experimental_specs_wl");

            migrationBuilder.DropColumn(
                name: "last_updated",
                table: "base_weights");
        }
    }
}
