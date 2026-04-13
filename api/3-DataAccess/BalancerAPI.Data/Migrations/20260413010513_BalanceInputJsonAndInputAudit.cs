using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class BalanceInputJsonAndInputAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "input",
                table: "experimental_balance_log",
                type: "jsonb",
                nullable: true);

            migrationBuilder.RenameColumn(
                name: "inputted",
                table: "experimental_balance_log",
                newName: "counted");

            migrationBuilder.DropTable(
                name: "experimental_input_log");

            migrationBuilder.CreateTable(
                name: "experimental_input_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    balance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<string>(type: "character(24)", fixedLength: true, maxLength: 24, nullable: false),
                    action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experimental_input_log", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "experimental_input_log");

            migrationBuilder.CreateTable(
                name: "experimental_input_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    balance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    input = table.Column<string>(type: "jsonb", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    inputted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experimental_input_log", x => x.id);
                });

            migrationBuilder.DropColumn(
                name: "input",
                table: "experimental_balance_log");

            migrationBuilder.RenameColumn(
                name: "counted",
                table: "experimental_balance_log",
                newName: "inputted");
        }
    }
}
