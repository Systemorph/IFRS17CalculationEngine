using Microsoft.Extensions.Logging;
using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Utils;

public static class ApplicationMessage
{
    private static ILogger log;

    private static IActivityVariable activity;

    public static void Configure(ILogger log, IActivityVariable activity)
    {
        ApplicationMessage.log = log;
        ApplicationMessage.activity = activity;
    }

    public static object Log(Error e, params string[] s)
    {
        log.LogError(e.GetMessage(s));
        return null;
    }

    public static object Log(Warning w, params string[] s)
    {
        log.LogWarning(w.GetMessage(s));
        return null;
    }

    public static object Log(ActivityLog activityLog)
    {
        foreach (var error in activityLog.Errors) log.LogError(error.ToString());
        foreach (var warning in activityLog.Warnings) log.LogWarning(warning.ToString());
        return null;
    }

    public static bool HasErrors()
    {
        return activity.HasErrors();
    }

    public static bool HasWarnings()
    {
        return activity.HasWarnings();
    }
}

//ApplicationMessage.Configure(Log, Activity)

public static class ActivityLogUtils
{
    public static bool HasErrors(this ActivityLog[] logs)
    {
        foreach (var log in logs)
            if (log.Errors.Any())
                return true;
        return false;
    }


    public static ActivityLog Merge(this ActivityLog a, ActivityLog b)
    {
        return a with
        {
            Status = a.Status == ActivityLogStatus.Failed || b.Status == ActivityLogStatus.Failed ? ActivityLogStatus.Failed : ActivityLogStatus.Succeeded,
            StartDateTime = a.StartDateTime < b.StartDateTime ? a.StartDateTime : b.StartDateTime,
            FinishDateTime = a.FinishDateTime > b.FinishDateTime ? a.FinishDateTime : b.FinishDateTime,
            Errors = a.Errors.Concat(b.Errors).ToHashSet().ToList(),
            Warnings = a.Warnings.Concat(b.Warnings).ToHashSet().ToList(),
            Infos = a.Infos.Concat(b.Infos).ToHashSet().ToList()
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