using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanhTourGuide.WebAdmin.Migrations
{
    /// <inheritdoc />
    public partial class AddListeningLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Chỉ tạo bảng ListeningLog, bỏ qua bảng Poi đã tồn tại
            migrationBuilder.CreateTable(
                name: "ListeningLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoiId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnonymousSessionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DurationSeconds = table.Column<double>(type: "float", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    ListenAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListeningLog", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Khi rollback (lùi database), chỉ xóa bảng ListeningLog
            migrationBuilder.DropTable(
                name: "ListeningLog");
        }
    }
}