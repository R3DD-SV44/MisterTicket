using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MisterTicket.Server.Migrations
{
    /// <inheritdoc />
    public partial class FixDbSetNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Stadia_SceneId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceZones_Stadia_SceneId",
                table: "PriceZones");

            migrationBuilder.DropForeignKey(
                name: "FK_Seats_Stadia_SceneId",
                table: "Seats");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Stadia",
                table: "Stadia");

            migrationBuilder.RenameTable(
                name: "Stadia",
                newName: "Scene");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Scene",
                table: "Scene",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Scene_SceneId",
                table: "Events",
                column: "SceneId",
                principalTable: "Scene",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceZones_Scene_SceneId",
                table: "PriceZones",
                column: "SceneId",
                principalTable: "Scene",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Seats_Scene_SceneId",
                table: "Seats",
                column: "SceneId",
                principalTable: "Scene",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Scene_SceneId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceZones_Scene_SceneId",
                table: "PriceZones");

            migrationBuilder.DropForeignKey(
                name: "FK_Seats_Scene_SceneId",
                table: "Seats");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Scene",
                table: "Scene");

            migrationBuilder.RenameTable(
                name: "Scene",
                newName: "Stadia");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Stadia",
                table: "Stadia",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Stadia_SceneId",
                table: "Events",
                column: "SceneId",
                principalTable: "Stadia",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceZones_Stadia_SceneId",
                table: "PriceZones",
                column: "SceneId",
                principalTable: "Stadia",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Seats_Stadia_SceneId",
                table: "Seats",
                column: "SceneId",
                principalTable: "Stadia",
                principalColumn: "Id");
        }
    }
}
