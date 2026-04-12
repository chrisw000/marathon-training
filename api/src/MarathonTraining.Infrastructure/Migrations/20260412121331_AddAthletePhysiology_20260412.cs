using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarathonTraining.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAthletePhysiology_20260412 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AthleteProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RestingHr = table.Column<int>(type: "int", nullable: true),
                    MaxHr = table.Column<int>(type: "int", nullable: true),
                    ThresholdHr = table.Column<int>(type: "int", nullable: true),
                    FtpWatts = table.Column<int>(type: "int", nullable: true),
                    CurrentPhase = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Base"),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AthleteProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StravaConnections",
                columns: table => new
                {
                    AthleteProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StravaAthleteId = table.Column<long>(type: "bigint", nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StravaConnections", x => x.AthleteProfileId);
                    table.ForeignKey(
                        name: "FK_StravaConnections_AthleteProfiles_AthleteProfileId",
                        column: x => x.AthleteProfileId,
                        principalTable: "AthleteProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AthleteProfiles_UserId",
                table: "AthleteProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StravaConnections");

            migrationBuilder.DropTable(
                name: "AthleteProfiles");
        }
    }
}
