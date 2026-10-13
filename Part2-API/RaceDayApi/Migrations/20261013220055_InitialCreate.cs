using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDayApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    full_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    email = table.Column<string>(type: "varchar(150)", nullable: false),
                    password_hash = table.Column<string>(type: "varchar(255)", nullable: false),
                    role = table.Column<string>(type: "varchar(20)", nullable: false),
                    phone_number = table.Column<string>(type: "varchar(20)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.user_id);
                    table.CheckConstraint("CK_Users_Role", "[role] IN ('Organiser','Participant')");
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    event_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    organiser_id = table.Column<int>(type: "int", nullable: false),
                    event_name = table.Column<string>(type: "varchar(150)", nullable: false),
                    description = table.Column<string>(type: "varchar(1000)", nullable: true),
                    event_date = table.Column<DateOnly>(type: "date", nullable: false),
                    event_type = table.Column<string>(type: "varchar(20)", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Upcoming"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.event_id);
                    table.CheckConstraint("CK_Events_Status", "[status] IN ('Upcoming','Completed','Cancelled')");
                    table.CheckConstraint("CK_Events_Type", "[event_type] IN ('Run','Walk','Cycle')");
                    table.ForeignKey(
                        name: "FK_Events_Users_organiser_id",
                        column: x => x.organiser_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    category_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    event_id = table.Column<int>(type: "int", nullable: false),
                    category_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    distance_km = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    price = table.Column<decimal>(type: "decimal(8,2)", nullable: false, defaultValue: 0m),
                    max_participants = table.Column<int>(type: "int", nullable: false, defaultValue: 100)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.category_id);
                    table.ForeignKey(
                        name: "FK_Categories_Events_event_id",
                        column: x => x.event_id,
                        principalTable: "Events",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Venues",
                columns: table => new
                {
                    venue_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    event_id = table.Column<int>(type: "int", nullable: false),
                    venue_name = table.Column<string>(type: "varchar(150)", nullable: false),
                    address = table.Column<string>(type: "varchar(255)", nullable: false),
                    city = table.Column<string>(type: "varchar(100)", nullable: false),
                    latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venues", x => x.venue_id);
                    table.ForeignKey(
                        name: "FK_Venues_Events_event_id",
                        column: x => x.event_id,
                        principalTable: "Events",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Enrolments",
                columns: table => new
                {
                    enrolment_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    participant_id = table.Column<int>(type: "int", nullable: false),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    enrolment_date = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    bib_number = table.Column<string>(type: "varchar(10)", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Confirmed")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrolments", x => x.enrolment_id);
                    table.CheckConstraint("CK_Enrolments_Status", "[status] IN ('Confirmed','Cancelled')");
                    table.ForeignKey(
                        name: "FK_Enrolments_Categories_category_id",
                        column: x => x.category_id,
                        principalTable: "Categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrolments_Users_participant_id",
                        column: x => x.participant_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    result_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    enrolment_id = table.Column<int>(type: "int", nullable: false),
                    finish_time = table.Column<TimeSpan>(type: "time", nullable: true),
                    position = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Finished"),
                    recorded_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.result_id);
                    table.CheckConstraint("CK_Results_Status", "[status] IN ('Finished','DNF','DNS')");
                    table.ForeignKey(
                        name: "FK_Results_Enrolments_enrolment_id",
                        column: x => x.enrolment_id,
                        principalTable: "Enrolments",
                        principalColumn: "enrolment_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_event_id",
                table: "Categories",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_category_id",
                table: "Enrolments",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "UQ_Enrolment_Once",
                table: "Enrolments",
                columns: new[] { "participant_id", "category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_organiser_id",
                table: "Events",
                column: "organiser_id");

            migrationBuilder.CreateIndex(
                name: "IX_Results_enrolment_id",
                table: "Results",
                column: "enrolment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_email",
                table: "Users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venues_event_id",
                table: "Venues",
                column: "event_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Venues");

            migrationBuilder.DropTable(
                name: "Enrolments");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
