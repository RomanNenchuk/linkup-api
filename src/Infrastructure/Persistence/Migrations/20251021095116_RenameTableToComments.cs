using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameTableToComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostCommnets_AspNetUsers_AuthorId",
                table: "PostCommnets");

            migrationBuilder.DropForeignKey(
                name: "FK_PostCommnets_Posts_PostId",
                table: "PostCommnets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PostCommnets",
                table: "PostCommnets");

            migrationBuilder.RenameTable(
                name: "PostCommnets",
                newName: "PostComments");

            migrationBuilder.RenameIndex(
                name: "IX_PostCommnets_PostId",
                table: "PostComments",
                newName: "IX_PostComments_PostId");

            migrationBuilder.RenameIndex(
                name: "IX_PostCommnets_AuthorId",
                table: "PostComments",
                newName: "IX_PostComments_AuthorId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PostComments",
                table: "PostComments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PostComments_AspNetUsers_AuthorId",
                table: "PostComments",
                column: "AuthorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostComments_Posts_PostId",
                table: "PostComments",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostComments_AspNetUsers_AuthorId",
                table: "PostComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PostComments_Posts_PostId",
                table: "PostComments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PostComments",
                table: "PostComments");

            migrationBuilder.RenameTable(
                name: "PostComments",
                newName: "PostCommnets");

            migrationBuilder.RenameIndex(
                name: "IX_PostComments_PostId",
                table: "PostCommnets",
                newName: "IX_PostCommnets_PostId");

            migrationBuilder.RenameIndex(
                name: "IX_PostComments_AuthorId",
                table: "PostCommnets",
                newName: "IX_PostCommnets_AuthorId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PostCommnets",
                table: "PostCommnets",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PostCommnets_AspNetUsers_AuthorId",
                table: "PostCommnets",
                column: "AuthorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostCommnets_Posts_PostId",
                table: "PostCommnets",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
