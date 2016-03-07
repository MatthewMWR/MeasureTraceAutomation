// Copyright and license at: https://github.com/MatthewMWR/MeasureTraceAutomation/blob/master/LICENSE
using System.Diagnostics;
using System.Linq;

namespace MeasureTraceAutomation.Logging
{
    public class ForwardMeasureTraceLogging : TraceListener
    {
        public override void Write(string message)
        {
            WriteLine(message);
        }

        public override void WriteLine(string message)
        {
            if (!message.StartsWith(MeasureTrace.Logging.MtDebugMessagePrefix)) return;
            SimpleDebugLog.LogThis(message);
        }

        public static void AddListenerAsNeeded()
        {
            if (!Trace.Listeners.OfType<ForwardMeasureTraceLogging>().Any())
                Trace.Listeners.Add(new ForwardMeasureTraceLogging());
        }
    }
}