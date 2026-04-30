using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class AdminEventFeedRepository(MedicalCenterDbContext dbContext) : IAdminEventFeedRepository
{
    public async Task AddAsync(AdminEventFeedEntry entry, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(cancellationToken))
        {
            return;
        }

        await dbContext.AdminEventFeedEntries.AddAsync(entry, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AdminEventFeedEntry>> ListAsync(
        int limit,
        DateTimeOffset? beforeOccurredAt,
        long? beforeId,
        Guid? actorUserId,
        IReadOnlyCollection<string> actionCodes,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(cancellationToken))
        {
            return Array.Empty<AdminEventFeedEntry>();
        }

        var normalizedActionCodes = actionCodes as string[] ?? actionCodes.ToArray();
        var query = dbContext.AdminEventFeedEntries.AsNoTracking().AsQueryable();

        if (actorUserId.HasValue)
        {
            query = query.Where(x => x.ActorUserId == actorUserId);
        }

        if (normalizedActionCodes.Length > 0)
        {
            query = query.Where(x => normalizedActionCodes.Contains(x.ActionCode));
        }

        if (dateFrom.HasValue)
        {
            var from = dateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.OccurredAt >= new DateTimeOffset(from));
        }

        if (dateTo.HasValue)
        {
            var to = dateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.OccurredAt < new DateTimeOffset(to));
        }

        if (beforeOccurredAt.HasValue && beforeId.HasValue)
        {
            query = query.Where(x => x.OccurredAt < beforeOccurredAt || (x.OccurredAt == beforeOccurredAt && x.Id < beforeId));
        }

        var safeLimit = Math.Clamp(limit, 1, 100);
        return await query
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminEventFeedFilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(cancellationToken))
        {
            return new AdminEventFeedFilterOptionsDto(Array.Empty<AdminEventFeedActorOptionDto>(), Array.Empty<AdminEventFeedActionOptionDto>());
        }

        var actorsRaw = await dbContext.AdminEventFeedEntries
            .AsNoTracking()
            .Where(x => x.ActorUserId != null)
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new { x.ActorUserId, x.ActorLabel })
            .ToListAsync(cancellationToken);

        var actors = actorsRaw
            .GroupBy(x => x.ActorUserId)
            .Select(group =>
            {
                var preferred = group.FirstOrDefault(x => !string.Equals(x.ActorLabel, "Sistema", StringComparison.OrdinalIgnoreCase));
                var label = preferred?.ActorLabel ?? group.First().ActorLabel ?? "Sistema";
                return new AdminEventFeedActorOptionDto(group.Key, label);
            })
            .OrderBy(x => x.Label)
            .ToArray();

        var actionsRaw = await dbContext.AdminEventFeedEntries
            .AsNoTracking()
            .Select(x => new { x.ActionCode, x.ActionFamily, x.Title })
            .ToListAsync(cancellationToken);

        var actions = actionsRaw
            .GroupBy(x => new { x.ActionCode, x.ActionFamily })
            .Select(group =>
            {
                var label = group
                    .Select(x => x.Title)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .OrderBy(x => x)
                    .FirstOrDefault() ?? group.Key.ActionCode;

                return new AdminEventFeedActionOptionDto(group.Key.ActionCode, group.Key.ActionFamily, label);
            })
            .OrderBy(x => x.Label)
            .ToList();

        return new AdminEventFeedFilterOptionsDto(actors, actions);
    }

    private async Task<bool> TableExistsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select 1
            from information_schema.tables
            where table_schema = 'public'
              and table_name = 'admin_event_feed'
            limit 1
            """;

        return await dbContext.Database
            .SqlQueryRaw<int>(sql)
            .AnyAsync(cancellationToken);
    }
}
