using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crpg.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerksData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set perk points for existing characters:
            // Perk points start at level 29 (level up to 30 gives first point).
            // Formula: max(0, level - 29)
            // attribute_points and skill_points are the unspent pool, not the total
            // earned, so we don't back-correct them here. The new formula in
            // ResetCharacterCharacteristics will apply on next respec.
            migrationBuilder.Sql(@"
                UPDATE characters
                SET perk_points = GREATEST(0, level - 29)
                WHERE level > 29;");

            // Clear selected perks (need to be re-chosen since perks are new)
            migrationBuilder.Sql(@"
                UPDATE characters
                SET selected_perks = '{}'
                WHERE selected_perks != '{}';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Clear selected perks
            migrationBuilder.Sql(@"
                UPDATE characters
                SET selected_perks = '{}'
                WHERE selected_perks != '{}';");

            // Revert perk points to 0
            migrationBuilder.Sql(@"
                UPDATE characters
                SET perk_points = 0
                WHERE perk_points != 0;");
        }
    }
}
