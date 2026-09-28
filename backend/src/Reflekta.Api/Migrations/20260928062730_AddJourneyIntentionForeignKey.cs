using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reflekta.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddJourneyIntentionForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Journeys_IntentionId",
                table: "Journeys",
                column: "IntentionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Journeys_Intentions_IntentionId",
                table: "Journeys",
                column: "IntentionId",
                principalTable: "Intentions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Journeys_Intentions_IntentionId",
                table: "Journeys");

            migrationBuilder.DropIndex(
                name: "IX_Journeys_IntentionId",
                table: "Journeys");
        }
    }
}
