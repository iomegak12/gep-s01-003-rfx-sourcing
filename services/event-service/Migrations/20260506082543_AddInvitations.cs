using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventService.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_suppliers",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    supplier_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    invited_by_user_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    invited_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_suppliers", x => new { x.event_id, x.supplier_id });
                    table.ForeignKey(
                        name: "FK_event_suppliers_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_suppliers_event_id",
                table: "event_suppliers",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_event_suppliers_supplier_id",
                table: "event_suppliers",
                column: "supplier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_suppliers");
        }
    }
}
