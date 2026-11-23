using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "verification_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_by = table.Column<Guid>(type: "uuid", nullable: false),
                    secret_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    redeemed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    redeemed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_verification_codes", x => x.id);
                    table.UniqueConstraint("ak_verification_codes_id_school_id", x => new { x.id, x.school_id });
                    table.CheckConstraint("ck_verification_codes_redemption", "(redeemed_at IS NULL) = (redeemed_by IS NULL)");
                    table.CheckConstraint("ck_verification_codes_revocation", "(revoked_at IS NULL AND revoked_by IS NULL AND revocation_reason IS NULL) OR (revoked_at IS NOT NULL AND revoked_by IS NOT NULL AND revocation_reason IS NOT NULL AND length(trim(revocation_reason)) > 0)");
                    table.CheckConstraint("ck_verification_codes_validity", "expires_at > created_at AND valid_until >= expires_at");
                    table.ForeignKey(
                        name: "fk_verification_codes_schools_school_id",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_verification_codes_users_issued_by",
                        column: x => x.issued_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_verification_codes_users_redeemed_by",
                        column: x => x.redeemed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_verification_codes_users_revoked_by",
                        column: x => x.revoked_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parent_school_verifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parent_school_verifications", x => x.id);
                    table.CheckConstraint("ck_parent_school_verifications_validity", "valid_until > verified_at");
                    table.ForeignKey(
                        name: "fk_parent_school_verifications_schools_school_id",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_parent_school_verifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_parent_school_verifications_verification_codes_code_id_scho",
                        columns: x => new { x.code_id, x.school_id },
                        principalTable: "verification_codes",
                        principalColumns: new[] { "id", "school_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_parent_school_verifications_code_id",
                table: "parent_school_verifications",
                column: "code_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_parent_school_verifications_code_id_school_id",
                table: "parent_school_verifications",
                columns: new[] { "code_id", "school_id" });

            migrationBuilder.CreateIndex(
                name: "ix_parent_school_verifications_school_id",
                table: "parent_school_verifications",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "ix_parent_school_verifications_user_id_school_id",
                table: "parent_school_verifications",
                columns: new[] { "user_id", "school_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_parent_school_verifications_user_id_school_id_valid_until",
                table: "parent_school_verifications",
                columns: new[] { "user_id", "school_id", "valid_until" });

            migrationBuilder.CreateIndex(
                name: "ix_verification_codes_issued_by",
                table: "verification_codes",
                column: "issued_by");

            migrationBuilder.CreateIndex(
                name: "ix_verification_codes_redeemed_by",
                table: "verification_codes",
                column: "redeemed_by");

            migrationBuilder.CreateIndex(
                name: "ix_verification_codes_revoked_by",
                table: "verification_codes",
                column: "revoked_by");

            migrationBuilder.CreateIndex(
                name: "ix_verification_codes_school_id_created_at",
                table: "verification_codes",
                columns: new[] { "school_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_verification_codes_secret_hash",
                table: "verification_codes",
                column: "secret_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parent_school_verifications");

            migrationBuilder.DropTable(
                name: "verification_codes");
        }
    }
}
