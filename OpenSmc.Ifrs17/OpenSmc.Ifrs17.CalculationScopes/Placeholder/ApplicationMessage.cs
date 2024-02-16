using Microsoft.Extensions.Logging;
using OpenSmc.Activities;
using OpenSmc.Ifrs17.DataTypes.Constants.Validations;

namespace OpenSms.Ifrs17.CalculationScopes.Placeholder;

public static class ApplicationMessage
{
    private static ILogger log;

    private static IActivityService activity;

    public static void Configure(ILogger log, IActivityService activity)
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
        foreach (var error in activityLog.Errors()) log.LogError(error.ToString());
        foreach (var warning in activityLog.Warnings()) log.LogWarning(warning.ToString());
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