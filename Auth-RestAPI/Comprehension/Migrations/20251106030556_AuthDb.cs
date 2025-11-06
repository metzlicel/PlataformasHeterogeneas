using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comprehension.Migrations
{
    /// <inheritdoc />
    public partial class AuthDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Event_Users_UserId",
                table: "Event");

            migrationBuilder.DropForeignKey(
                name: "FK_Reminder_Users_UserId",
                table: "Reminder");

            migrationBuilder.DropForeignKey(
                name: "FK_ResourceShares_Note_NoteId",
                table: "ResourceShares");

            migrationBuilder.DropIndex(
                name: "IX_ResourceShares_NoteId",
                table: "ResourceShares");

            migrationBuilder.DropColumn(
                name: "NoteId",
                table: "ResourceShares");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Reminder",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Event",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Event_Users_UserId",
                table: "Event",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reminder_Users_UserId",
                table: "Reminder",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Event_Users_UserId",
                table: "Event");

            migrationBuilder.DropForeignKey(
                name: "FK_Reminder_Users_UserId",
                table: "Reminder");

            migrationBuilder.AddColumn<Guid>(
                name: "NoteId",
                table: "ResourceShares",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Reminder",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Event",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceShares_NoteId",
                table: "ResourceShares",
                column: "NoteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Event_Users_UserId",
                table: "Event",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reminder_Users_UserId",
                table: "Reminder",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceShares_Note_NoteId",
                table: "ResourceShares",
                column: "NoteId",
                principalTable: "Note",
                principalColumn: "Id");
        }
    }
}
