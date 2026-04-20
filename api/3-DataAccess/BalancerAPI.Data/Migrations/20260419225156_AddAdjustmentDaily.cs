using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalancerAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAdjustmentDaily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adjustment_daily",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    trajectory = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adjustment_daily", x => x.uuid);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adjustment_daily");
        }
    }
}
