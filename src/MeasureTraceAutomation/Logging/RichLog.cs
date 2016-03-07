// Copyright and license at: https://github.com/MatthewMWR/MeasureTraceAutomation/blob/master/LICENSE
using System.Diagnostics.Tracing;

namespace MeasureTraceAutomation.Logging
{
    [EventSource(Name = "MeasureTraceAutomation")]
    public sealed partial class RichLog : EventSource
    {
        public static RichLog Log = new RichLog();

        [Event(10000, Level = EventLevel.Verbose, Channel = EventChannel.Operational, Message = "DebugMessage: {0}")]
        public void DebugMessageToOperationalChannel(string message)
        {
            WriteEvent(10000, message);
        }

        [Event(10001, Level = EventLevel.Verbose, Message = "DebugMessage: {0}")]
        public void DebugMessage(string message)
        {
            WriteEvent(10001, message);
        }

        public class Tasks
        {
            public const EventTask None = (EventTask) 0x1;
            public const EventTask ConsolidateFiles = (EventTask) 0x2;
            public const EventTask SaveToDatabase = (EventTask) 0x4;
            public const EventTask LoadFromDatabase = (EventTask) 0x8;
            public const EventTask DiscoverFiles = (EventTask) 0x16;
            public const EventTask MeasureTrace = (EventTask) 0x32;
            public const EventTask ProcessEndToEnd = (EventTask) 0x64;
            public const EventTask TraceAnalyzeFailureDuringProcessEndToEnd = (EventTask) 0x128;
            public const EventTask ProcessingTaskAborted = (EventTask) 0x256;
            public const EventTask MeasureAndSaveItem = (EventTask) 0x512;
        }
    }
}