using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MisterTicket.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSeatsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "Seats");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "Users",
                newName: "Password");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Seats",
                newName: "PriceZoneId");

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EventSeats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    SeatId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReservedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventSeats_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventSeats_Seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "Seats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Seats_PriceZoneId",
                table: "Seats",
                column: "PriceZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSeats_EventId_SeatId",
                table: "EventSeats",
                columns: new[] { "EventId", "SeatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventSeats_SeatId",
                table: "EventSeats",
                column: "SeatId");

            migrationBuilder.AddForeignKey(
                name: "FK_Seats_PriceZones_PriceZoneId",
                table: "Seats",
                column: "PriceZoneId",
                principalTable: "PriceZones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Seats_PriceZones_PriceZoneId",
                table: "Seats");

            migrationBuilder.DropTable(
                name: "EventSeats");

            migrationBuilder.DropIndex(
                name: "IX_Seats_PriceZoneId",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Reservations");

            migrationBuilder.RenameColumn(
                name: "Password",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "PriceZoneId",
                table: "Seats",
                newName: "Status");

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "Seats",
                type: "datetime2",
                nullable: true);
        }
    }
}
