using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    activities_rating = table.Column<int>(type: "integer", nullable: false),
                    communication_rating = table.Column<int>(type: "integer", nullable: false),
                    cons = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    facilities_rating = table.Column<int>(type: "integer", nullable: false),
                    overall_rating = table.Column<int>(type: "integer", nullable: false),
                    pros = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    review_text = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    safety_rating = table.Column<int>(type: "integer", nullable: false),
                    teaching_rating = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviews", x => x.id);
                    table.CheckConstraint("ck_reviews_ratings", "overall_rating BETWEEN 1 AND 5 AND teaching_rating BETWEEN 1 AND 5 AND safety_rating BETWEEN 1 AND 5 AND communication_rating BETWEEN 1 AND 5 AND facilities_rating BETWEEN 1 AND 5 AND activities_rating BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_reviews_status", "status IN ('PendingModeration', 'Published', 'Rejected')");
                    table.CheckConstraint("ck_reviews_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_reviews_schools_school_id",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reviews_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "review_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    moderation_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    withdrawn_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    activities_rating = table.Column<int>(type: "integer", nullable: false),
                    communication_rating = table.Column<int>(type: "integer", nullable: false),
                    cons = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    facilities_rating = table.Column<int>(type: "integer", nullable: false),
                    overall_rating = table.Column<int>(type: "integer", nullable: false),
                    pros = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    review_text = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    safety_rating = table.Column<int>(type: "integer", nullable: false),
                    teaching_rating = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_versions", x => x.id);
                    table.CheckConstraint("ck_review_versions_decision", "(status = 'PendingModeration' AND reviewed_at IS NULL AND reviewed_by IS NULL) OR (status IN ('Published', 'Rejected') AND reviewed_at IS NOT NULL AND reviewed_by IS NOT NULL)");
                    table.CheckConstraint("ck_review_versions_ratings", "overall_rating BETWEEN 1 AND 5 AND teaching_rating BETWEEN 1 AND 5 AND safety_rating BETWEEN 1 AND 5 AND communication_rating BETWEEN 1 AND 5 AND facilities_rating BETWEEN 1 AND 5 AND activities_rating BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_review_versions_status", "status IN ('PendingModeration', 'Published', 'Rejected')");
                    table.CheckConstraint("ck_review_versions_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_review_versions_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_review_versions_users_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_review_versions_review_id_version",
                table: "review_versions",
                columns: new[] { "review_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_review_versions_reviewed_by",
                table: "review_versions",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_school_id_status_updated_at",
                table: "reviews",
                columns: new[] { "school_id", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reviews_status_updated_at",
                table: "reviews",
                columns: new[] { "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reviews_user_id_school_id",
                table: "reviews",
                columns: new[] { "user_id", "school_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "review_versions");

            migrationBuilder.DropTable(
                name: "reviews");
        }
    }
}
