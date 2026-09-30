using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reflekta.Api.Migrations
{
    /// <inheritdoc />
    public partial class conversation_messages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConversationMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayedCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationMessages_PlayedCards_PlayedCardId",
                        column: x => x.PlayedCardId,
                        principalTable: "PlayedCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_PlayedCardId_CreatedAt",
                table: "ConversationMessages",
                columns: new[] { "PlayedCardId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationMessages");
        }
    }
}
