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

    public async Task<IReadOnlyCollection<AdminEventFeedEntry>> ListAsync(AdminEventFeedQuery query, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(cancellationToken))
        {
            return Array.Empty<AdminEventFeedEntry>();
        }

        var normalizedActionCodes = query.ActionCodes as string[] ?? query.ActionCodes.ToArray();
        var dbQuery = dbContext.AdminEventFeedEntries.AsNoTracking().AsQueryable();

        if (query.ActorUserId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.ActorUserId == query.ActorUserId);
        }

        if (normalizedActionCodes.Length > 0)
        {
            dbQuery = dbQuery.Where(x => normalizedActionCodes.Contains(x.ActionCode));
        }

        if (query.DateFrom.HasValue)
        {
            var from = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            dbQuery = dbQuery.Where(x => x.OccurredAt >= new DateTimeOffset(from));
        }

        if (query.DateTo.HasValue)
        {
            var to = query.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            dbQuery = dbQuery.Where(x => x.OccurredAt < new DateTimeOffset(to));
        }

        if (query.BeforeOccurredAt.HasValue && query.BeforeId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.OccurredAt < query.BeforeOccurredAt || (x.OccurredAt == query.BeforeOccurredAt && x.Id < query.BeforeId));
        }

        var safeLimit = Math.Clamp(query.Limit, 1, 100);
        return await dbQuery
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
