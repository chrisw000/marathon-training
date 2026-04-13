using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarathonTraining.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityStravaFields_20260412 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AveragePowerWatts",
                table: "Activities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AverageSpeedMetresPerSecond",
                table: "Activities",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSource",
                table: "Activities",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasHeartRate",
                table: "Activities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDevicePower",
                table: "Activities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "StravaActivityId",
                table: "Activities",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StravaActivityType",
                table: "Activities",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_StravaActivityId",
                table: "Activities",
                column: "StravaActivityId",
                unique: true,
                filter: "[StravaActivityId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Activities_StravaActivityId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "AveragePowerWatts",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "AverageSpeedMetresPerSecond",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ExternalSource",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "HasHeartRate",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "IsDevicePower",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "StravaActivityId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "StravaActivityType",
                table: "Activities");
        }
    }
}
