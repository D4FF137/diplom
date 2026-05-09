using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using UserService.Data;

#nullable disable

namespace UserService.Migrations
{
    [DbContext(typeof(UserDbContext))]
    [Migration("20260509000000_AddCompanyGroups")]
    public partial class AddCompanyGroups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "companygroups",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    companyid = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    leaderuserid = table.Column<int>(type: "integer", nullable: false),
                    chatid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    createdbyuserid = table.Column<int>(type: "integer", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companygroups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "companygroupmembers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    companygroupid = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    joinedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companygroupmembers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_companygroups_companyid",
                table: "companygroups",
                column: "companyid");

            migrationBuilder.CreateIndex(
                name: "IX_companygroups_companyid_name",
                table: "companygroups",
                columns: new[] { "companyid", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_companygroupmembers_companygroupid",
                table: "companygroupmembers",
                column: "companygroupid");

            migrationBuilder.CreateIndex(
                name: "IX_companygroupmembers_companygroupid_userid",
                table: "companygroupmembers",
                columns: new[] { "companygroupid", "userid" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "companygroupmembers");
            migrationBuilder.DropTable(name: "companygroups");
        }
    }
}
