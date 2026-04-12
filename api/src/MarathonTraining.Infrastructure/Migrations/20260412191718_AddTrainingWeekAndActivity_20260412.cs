using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarathonTraining.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingWeekAndActivity_20260412 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingWeeks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AthleteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeekStartDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingWeeks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainingWeekId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AthleteProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    DistanceMetres = table.Column<double>(type: "float", nullable: true),
                    TssScore = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    AverageHeartRateBpm = table.Column<int>(type: "int", nullable: true),
                    MaxHeartRateBpm = table.Column<int>(type: "int", nullable: true),
                    NormalisedPowerWatts = table.Column<int>(type: "int", nullable: true),
                    RpeValue = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_TrainingWeeks_TrainingWeekId",
                        column: x => x.TrainingWeekId,
                        principalTable: "TrainingWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_AthleteProfileId",
                table: "Activities",
                column: "AthleteProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_AthleteProfileId_StartedAt",
                table: "Activities",
                columns: new[] { "AthleteProfileId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_TrainingWeekId",
                table: "Activities",
                column: "TrainingWeekId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingWeeks_AthleteId_WeekStartDate",
                table: "TrainingWeeks",
                columns: new[] { "AthleteId", "WeekStartDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "TrainingWeeks");
        }
    }
}
