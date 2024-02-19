using System.Collections.Immutable;
using OpenSmc.Activities;

namespace OpenSmc.Ifrs17.CalculationScopes.Placeholder;

public static class ActivityLogUtils
{
    public static bool HasErrors(this ActivityLog[] logs)
    {
        foreach (var log in logs)
            if (log.Errors().Any())
                return true;
        return false;
    }


    public static ActivityLog Merge(this ActivityLog a, ActivityLog b)
    {
        return a with
        {
            Status = a.Status == ActivityLogStatus.Failed || b.Status ==
            ActivityLogStatus.Failed ? ActivityLogStatus.Failed : ActivityLogStatus.Succeeded,
            Messages = a.Messages.Concat(b.Messages).ToImmutableList()
        };
    }

    public static ActivityLog Merge(this ActivityLog[] logs)
    {
        if (logs == null || logs.Length == 0) return null;
        return logs.Aggregate((x, y) => x.Merge(y));
    }

    public static ActivityLog Merge(this ActivityLog[] logs, ActivityLog log)
    {
        return logs.Merge().Merge(log);
    }

    public static ActivityLog Merge(this ActivityLog log, ActivityLog[] logs)
    {
        return log.Merge(logs.Merge());
    }
}