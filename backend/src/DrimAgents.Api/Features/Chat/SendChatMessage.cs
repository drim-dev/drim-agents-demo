using DrimAgents.Api.Common.AgentGateway;
using DrimAgents.Api.Common.Exceptions;
using DrimAgents.Api.Common.Http;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Database;
using DrimAgents.Api.Domain.Chat;
using DrimAgents.Api.Domain.Tasks;
using DrimAgents.Api.Hubs;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DrimAgents.Api.Features.Chat;

public static class SendChatMessage
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/tasks/{taskId}/chat/messages", async (
                string taskId,
                HttpContext httpContext,
                [FromBody] Body body,
                ISender sender,
                CancellationToken ct) =>
            {
                if (!Base32Encoder.TryDecode(taskId, out var decodedTaskId))
                    return Results.BadRequest("Invalid task ID format");

                var userId = httpContext.GetUserId()!.Value;
                var request = new Request(decodedTaskId, userId, body.Content);
                var response = await sender.Send(request, ct);

                return Results.Created(
                    $"/api/tasks/{taskId}/chat/messages",
                    response);
            })
            .RequireAuthorization()
            .WithName("SendChatMessage")
            .WithTags("Chat")
            .Produces<Response>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status503ServiceUnavailable);
        }

        private record Body(string Content);
    }

    public record Request(long TaskId, long UserId, string Content) : IRequest<Response>;

    public record Response(string Id, string Role, string Content, DateTime CreatedAt);

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Content is required")
                .WithErrorCode("chat:message:content:required")
                .MaximumLength(50000)
                .WithMessage("Content must not exceed 50000 characters")
                .WithErrorCode("chat:message:content:too_long");
        }
    }

    public class RequestHandler : IRequestHandler<Request, Response>
    {
        private static readonly HashSet<TaskStage> ChatAllowedStages =
            [TaskStage.Design, TaskStage.Plan, TaskStage.Implementation];

        private readonly AppDbContext _db;
        private readonly IIdFactory _idFactory;
        private readonly IAgentGateway _agentGateway;
        private readonly IHubContext<ChatHub> _chatHub;

        public RequestHandler(AppDbContext db, IIdFactory idFactory, IAgentGateway agentGateway, IHubContext<ChatHub> chatHub)
        {
            _db = db;
            _idFactory = idFactory;
            _agentGateway = agentGateway;
            _chatHub = chatHub;
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

            if (!ChatAllowedStages.Contains(task.Stage))
                throw new BadRequestException("Chat is not available at the current task stage");

            if (!_agentGateway.IsAvailable)
                throw new ServiceUnavailableException("Agent is currently unavailable");

            var session = await _db.ChatSessions
                .Where(s => s.TaskId == request.TaskId && s.Stage == task.Stage)
                .FirstOrDefaultAsync(ct);

            if (session == null)
            {
                session = new ChatSession
                {
                    Id = _idFactory.CreateId(),
                    TaskId = request.TaskId,
                    Stage = task.Stage,
                    CreatedAt = DateTime.UtcNow
                };
                _db.ChatSessions.Add(session);
            }

            var message = new ChatMessage
            {
                Id = _idFactory.CreateId(),
                ChatSessionId = session.Id,
                Role = ChatMessageRole.User,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            _db.ChatMessages.Add(message);
            await _db.SaveChangesAsync(ct);

            var encodedTaskId = Base32Encoder.Encode(request.TaskId);

            await _agentGateway.SendMessageAsync(
                encodedTaskId,
                Base32Encoder.Encode(session.Id),
                session.ClaudeSessionId,
                request.Content,
                ct);

            var response = new Response(
                Base32Encoder.Encode(message.Id),
                message.Role.ToString(),
                message.Content,
                message.CreatedAt);

            await _chatHub.Clients.Group($"task:{encodedTaskId}")
                .SendAsync("MessageReceived", new
                {
                    taskId = encodedTaskId,
                    message = new
                    {
                        id = response.Id,
                        role = response.Role,
                        content = response.Content,
                        createdAt = response.CreatedAt
                    }
                }, ct);

            return response;
        }
    }
}
