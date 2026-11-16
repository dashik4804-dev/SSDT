using System;
using ClubPlatform.Shared.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddClubBans : Migration
    {
        /// <inheritdoc />

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Создаём только таблицу блокировок клубов.
            // Остальные таблицы уже существуют в PostgreSQL.

            migrationBuilder.CreateTable(
                name: "club_bans",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    club_id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    user_id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    banned_by_user_id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    reason = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false),

                    created_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_club_bans",
                        x => x.id);

                    table.ForeignKey(
                        name: "FK_club_bans_clubs_club_id",
                        column: x => x.club_id,
                        principalSchema: "public",
                        principalTable: "clubs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_club_bans_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Запрещаем создавать несколько блокировок
            // одного пользователя в одном клубе.
            migrationBuilder.CreateIndex(
                name: "IX_club_bans_club_id_user_id",
                schema: "public",
                table: "club_bans",
                columns: new[] { "club_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_club_bans_user_id",
                schema: "public",
                table: "club_bans",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // При откате удаляем только таблицу блокировок.
            migrationBuilder.DropTable(
                name: "club_bans",
                schema: "public");
        }

    }
}
