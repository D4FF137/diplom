using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using FeedService.Data;

#nullable disable

namespace FeedService.Migrations
{
    [DbContext(typeof(FeedDbContext))]
    [Migration("20240101000000_InitialCreate")]
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "posts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    companyid = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    imageurl = table.Column<string>(type: "text", nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_posts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "likes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    companyid = table.Column<int>(type: "integer", nullable: false),
                    postid = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_likes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    companyid = table.Column<int>(type: "integer", nullable: false),
                    postid = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_posts_companyid",
                table: "posts",
                column: "companyid");

            migrationBuilder.CreateIndex(
                name: "IX_posts_userid",
                table: "posts",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_likes_companyid",
                table: "likes",
                column: "companyid");

            migrationBuilder.CreateIndex(
                name: "IX_likes_postid",
                table: "likes",
                column: "postid");

            migrationBuilder.CreateIndex(
                name: "IX_likes_userid",
                table: "likes",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_likes_postid_userid",
                table: "likes",
                columns: new[] { "postid", "userid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_comments_companyid",
                table: "comments",
                column: "companyid");

            migrationBuilder.CreateIndex(
                name: "IX_comments_postid",
                table: "comments",
                column: "postid");

            migrationBuilder.CreateIndex(
                name: "IX_comments_userid",
                table: "comments",
                column: "userid");

            // Enable Row Level Security
            migrationBuilder.Sql("ALTER TABLE posts ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE likes ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE comments ENABLE ROW LEVEL SECURITY;");

            // Create policies for company isolation
            migrationBuilder.Sql(@"
                CREATE POLICY company_isolation_policy ON posts
                    FOR ALL
                    USING (companyid = current_setting('app.current_company_id', true)::int);
            ");

            migrationBuilder.Sql(@"
                CREATE POLICY company_isolation_policy ON likes
                    FOR ALL
                    USING (companyid = current_setting('app.current_company_id', true)::int);
            ");

            migrationBuilder.Sql(@"
                CREATE POLICY company_isolation_policy ON comments
                    FOR ALL
                    USING (companyid = current_setting('app.current_company_id', true)::int);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comments");

            migrationBuilder.DropTable(
                name: "likes");

            migrationBuilder.DropTable(
                name: "posts");
        }
    }
}


