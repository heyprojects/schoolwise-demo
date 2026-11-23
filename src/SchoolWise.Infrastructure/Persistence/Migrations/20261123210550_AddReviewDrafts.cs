using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_reviews_ratings",
                table: "reviews");

            migrationBuilder.DropCheckConstraint(
                name: "ck_reviews_status",
                table: "reviews");

            migrationBuilder.DropCheckConstraint(
                name: "ck_review_versions_decision",
                table: "review_versions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_review_versions_ratings",
                table: "review_versions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_review_versions_status",
                table: "review_versions");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reviews_ratings",
                table: "reviews",
                sql: "(status = 'Draft' AND overall_rating BETWEEN 0 AND 5 AND teaching_rating BETWEEN 0 AND 5 AND safety_rating BETWEEN 0 AND 5 AND communication_rating BETWEEN 0 AND 5 AND facilities_rating BETWEEN 0 AND 5 AND activities_rating BETWEEN 0 AND 5) OR (status <> 'Draft' AND overall_rating BETWEEN 1 AND 5 AND teaching_rating BETWEEN 1 AND 5 AND safety_rating BETWEEN 1 AND 5 AND communication_rating BETWEEN 1 AND 5 AND facilities_rating BETWEEN 1 AND 5 AND activities_rating BETWEEN 1 AND 5)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reviews_status",
                table: "reviews",
                sql: "status IN ('Draft', 'PendingModeration', 'Published', 'Rejected')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_review_versions_decision",
                table: "review_versions",
                sql: "(status IN ('Draft', 'PendingModeration') AND reviewed_at IS NULL AND reviewed_by IS NULL) OR (status IN ('Published', 'Rejected') AND reviewed_at IS NOT NULL AND reviewed_by IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_review_versions_ratings",
                table: "review_versions",
                sql: "(status = 'Draft' AND overall_rating BETWEEN 0 AND 5 AND teaching_rating BETWEEN 0 AND 5 AND safety_rating BETWEEN 0 AND 5 AND communication_rating BETWEEN 0 AND 5 AND facilities_rating BETWEEN 0 AND 5 AND activities_rating BETWEEN 0 AND 5) OR (status <> 'Draft' AND overall_rating BETWEEN 1 AND 5 AND teaching_rating BETWEEN 1 AND 5 AND safety_rating BETWEEN 1 AND 5 AND communication_rating BETWEEN 1 AND 5 AND facilities_rating BETWEEN 1 AND 5 AND activities_rating BETWEEN 1 AND 5)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_review_versions_status",
                table: "review_versions",
                sql: "status IN ('Draft', 'PendingModeration', 'Published', 'Rejected')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_reviews_ratings",
                table: "reviews");

            migrationBuilder.DropCheckConstraint(
                name: "ck_reviews_status",
                table: "reviews");

            migrationBuilder.DropCheckConstraint(
                name: "ck_review_versions_decision",
                table: "review_versions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_review_versions_ratings",
                table: "review_versions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_review_versions_status",
                table: "review_versions");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reviews_ratings",
                table: "reviews",
                sql: "overall_rating BETWEEN 1 AND 5 AND teaching_rating BETWEEN 1 AND 5 AND safety_rating BETWEEN 1 AND 5 AND communication_rating BETWEEN 1 AND 5 AND facilities_rating BETWEEN 1 AND 5 AND activities_rating BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reviews_status",
                table: "reviews",
                sql: "status IN ('PendingModeration', 'Published', 'Rejected')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_review_versions_decision",
                table: "review_versions",
                sql: "(status = 'PendingModeration' AND reviewed_at IS NULL AND reviewed_by IS NULL) OR (status IN ('Published', 'Rejected') AND reviewed_at IS NOT NULL AND reviewed_by IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_review_versions_ratings",
                table: "review_versions",
                sql: "overall_rating BETWEEN 1 AND 5 AND teaching_rating BETWEEN 1 AND 5 AND safety_rating BETWEEN 1 AND 5 AND communication_rating BETWEEN 1 AND 5 AND facilities_rating BETWEEN 1 AND 5 AND activities_rating BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "ck_review_versions_status",
                table: "review_versions",
                sql: "status IN ('PendingModeration', 'Published', 'Rejected')");
        }
    }
}
