using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSEBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAbortFieldsToCorrectiveAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AbortReason",
                table: "CorrectiveActions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AbortedAt",
                table: "CorrectiveActions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AbortedById",
                table: "CorrectiveActions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveActions_AbortedById",
                table: "CorrectiveActions",
                column: "AbortedById");

            migrationBuilder.AddForeignKey(
                name: "FK_CorrectiveActions_AspNetUsers_AbortedById",
                table: "CorrectiveActions",
                column: "AbortedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CorrectiveActions_AspNetUsers_AbortedById",
                table: "CorrectiveActions");

            migrationBuilder.DropIndex(
                name: "IX_CorrectiveActions_AbortedById",
                table: "CorrectiveActions");

            migrationBuilder.DropColumn(
                name: "AbortReason",
                table: "CorrectiveActions");

            migrationBuilder.DropColumn(
                name: "AbortedAt",
                table: "CorrectiveActions");

            migrationBuilder.DropColumn(
                name: "AbortedById",
                table: "CorrectiveActions");
        }
    }
}
