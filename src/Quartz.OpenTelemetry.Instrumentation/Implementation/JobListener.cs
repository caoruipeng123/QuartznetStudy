using System;
using System.Diagnostics;

using OpenTelemetry.Instrumentation;
using OpenTelemetry.Trace;

namespace Quartz.OpenTelemetry.Instrumentation.Implementation
{
    internal sealed class JobListener : ListenerHandler
    {
        private readonly QuartzInstrumentationOptions options;
        private readonly ActivitySourceAdapter activitySource;

        public JobListener(string sourceName, QuartzInstrumentationOptions options, ActivitySourceAdapter activitySource) 
            : base(sourceName)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.activitySource = activitySource;
        }

        public override void OnStartActivity(Activity activity, object payload)
        {
            if (!(payload is IJobExecutionContext jobExecutionContext))
            {
                QuartzInstrumentationEventSource.Log.NullPayload(nameof(JobListener), nameof(OnStartActivity));
                return;
            }

            activity.SetKind(ActivityKind.Server);
            activitySource.Start(activity);

            if (activity.IsAllDataRequested)
            {
                //SetActivityAttributes(activity, jobExecutionContext, exception: null);
            }
        }

        public override void OnStopActivity(Activity activity, object payload)
        {
            if (!(payload is Tuple<IJobExecutionContext, Exception?> tuple))
            {
                QuartzInstrumentationEventSource.Log.NullPayload(nameof(JobListener), nameof(OnStopActivity));
                return;
            }

            var (_, exception) = tuple;
            if (exception != null)
            {
                activity.AddTag("error", "true");
                if (options.IncludeExceptionDetails)
                {
                    activity.AddTag("error.message", exception.Message);
                    activity.AddTag("error.stack", exception.StackTrace);
                }
            }
        }
    }
}