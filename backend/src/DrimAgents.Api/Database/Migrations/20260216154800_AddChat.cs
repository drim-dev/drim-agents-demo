using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DrimAgents.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "chat");

            migrationBuilder.EnsureSchema(
                name: "tasks");

            migrationBuilder.CreateTable(
                name: "tasks",
                schema: "tasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    Stage = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tasks_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "projects",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_sessions",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaskId = table.Column<long>(type: "bigint", nullable: false),
                    Stage = table.Column<string>(type: "text", nullable: false),
                    ClaudeSessionId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_sessions_tasks_TaskId",
                        column: x => x.TaskId,
                        principalSchema: "tasks",
                        principalTable: "tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatSessionId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_messages_chat_sessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalSchema: "chat",
                        principalTable: "chat_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_chat_session_id_created_at",
                schema: "chat",
                table: "chat_messages",
                columns: new[] { "ChatSessionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_chat_session_id_id",
                schema: "chat",
                table: "chat_messages",
                columns: new[] { "ChatSessionId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_sessions_task_id_stage",
                schema: "chat",
                table: "chat_sessions",
                columns: new[] { "TaskId", "Stage" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tasks_project_id",
                schema: "tasks",
                table: "tasks",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_messages",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "chat_sessions",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "tasks",
                schema: "tasks");
        }
    }
}
