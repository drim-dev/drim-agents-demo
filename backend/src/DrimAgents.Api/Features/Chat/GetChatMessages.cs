using DrimAgents.Api.Common.Exceptions;
using DrimAgents.Api.Common.Http;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Database;
using DrimAgents.Api.Domain.Chat;
using DrimAgents.Api.Domain.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DrimAgents.Api.Features.Chat;

public static class GetChatMessages
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/tasks/{taskId}/chat/messages", async (
                string taskId,
                string? stage,
                long? afterId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken ct) =>
            {
                if (!Base32Encoder.TryDecode(taskId, out var decodedTaskId))
                    return Results.BadRequest("Invalid task ID format");

                TaskStage? parsedStage = null;
                if (stage != null && !Enum.TryParse<TaskStage>(stage, ignoreCase: true, out var s))
                    return Results.BadRequest("Invalid stage value");
                else if (stage != null)
                    parsedStage = Enum.Parse<TaskStage>(stage, ignoreCase: true);

                var userId = httpContext.GetUserId()!.Value;
                var request = new Request(decodedTaskId, userId, parsedStage, afterId);
                var response = await sender.Send(request, ct);

                return Results.Ok(response);
            })
            .RequireAuthorization()
            .WithName("GetChatMessages")
            .WithTags("Chat")
            .Produces<Response>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
        }
    }

    public record Request(long TaskId, long UserId, TaskStage? Stage, long? AfterId) : IRequest<Response>;

    public record Response(string? ChatSessionId, string Stage, MessageItem[] Messages);

    public record MessageItem(string Id, string Role, string Content, DateTime CreatedAt);

    public class RequestHandler : IRequestHandler<Request, Response>
    {
        private readonly AppDbContext _db;

        public RequestHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Response> Handle(Request request, CancellationToken ct)
        {
            var task = await _db.ProjectTasks
                .AsNoTracking()
                .Where(t => t.Id == request.TaskId)
                .Select(t => new { t.Id, t.Stage, t.ProjectId })
                .FirstOrDefaultAsync(ct);

            if (task == null)
                throw new NotFoundException("Task", request.TaskId);

            var project = await _db.Projects
                .AsNoTracking()
                .Where(p => p.Id == task.ProjectId && p.UserId == request.UserId)
                .Select(p => new { p.Id })
                .FirstOrDefaultAsync(ct);

            if (project == null)
                throw new NotFoundException("Task", request.TaskId);

            var stage = request.Stage ?? task.Stage;

            var session = await _db.ChatSessions
                .AsNoTracking()
                .Where(s => s.TaskId == request.TaskId && s.Stage == stage)
                .Select(s => new { s.Id })
                .FirstOrDefaultAsync(ct);

            if (session == null)
            {
                return new Response(null, stage.ToString(), []);
            }

            var query = _db.ChatMessages
                .AsNoTracking()
                .Where(m => m.ChatSessionId == session.Id);

            if (request.AfterId.HasValue)
                query = query.Where(m => m.Id > request.AfterId.Value);

            var messages = await query
                .OrderBy(m => m.Id)
                .Select(m => new MessageItem(
                    Base32Encoder.Encode(m.Id),
                    m.Role.ToString(),
                    m.Content,
                    m.CreatedAt))
                .ToArrayAsync(ct);

            return new Response(
                Base32Encoder.Encode(session.Id),
                stage.ToString(),
                messages);
        }
    }
}
