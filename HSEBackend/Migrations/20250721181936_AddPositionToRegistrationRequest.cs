using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HSEBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionToRegistrationRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "RegistrationRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-default-id",
                columns: new[] { "NormalizedUserName", "UserName" },
                values: new object[] { "ADMIN@TE.COM", "admin@te.com" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "hse-default-id",
                columns: new[] { "NormalizedUserName", "UserName" },
                values: new object[] { "HSE@TE.COM", "hse@te.com" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "profile-user-1",
                columns: new[] { "NormalizedUserName", "UserName" },
                values: new object[] { "JOHN.DOE@TE.COM", "john.doe@te.com" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "profile-user-2",
                columns: new[] { "NormalizedUserName", "UserName" },
                values: new object[] { "JANE.SMITH@TE.COM", "jane.smith@te.com" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "RegistrationRequests");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-default-id",
                columns: new[] { "NormalizedUserName", "UserName" },
                values: new object[] { "ADMIN", "admin" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "hse-default-id",
                columns: new[] { "NormalizedUserName", "UserName" },
                values: new object[] { "HSE.MANAGER", "hse.manager" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "profile-user-1",
                columns: new[] { "NormalizedUserName", "UserName" },
                values: new object[] { "TE001234", "TE001234" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "profile-user-2",
                columns: new[] { "NormalizedUserName", "UserName" },
                values: new object[] { "TE005678", "TE005678" });
        }
    }
}
