// Copyright and license at: https://github.com/MatthewMWR/MeasureTraceAutomation/blob/master/LICENSE
using System.Diagnostics.Tracing;

namespace MeasureTraceAutomation.Logging
{
    public sealed partial class RichLog
    {
        [Event(101, Level = EventLevel.Verbose, Channel = EventChannel.Operational,
            Message = "StartFileDiscovery \nFrom:{0}", Task = Tasks.DiscoverFiles, Opcode = EventOpcode.Start)]
        public void StartDiscoverFiles(string path)
        {
            WriteEvent(101, path);
        }

        [Event(102, Level = EventLevel.Verbose, Channel = EventChannel.Operational,
            Message = "StopFileDiscovery \nFrom:{0} ; \nFiles:{1}", Task = Tasks.DiscoverFiles,
            Opcode = EventOpcode.Stop)]
        public void StopDiscoverFiles(string path, int fileCount)
        {
            WriteEvent(102, path, fileCount);
        }

        [Event(103, Level = EventLevel.Verbose, Channel = EventChannel.Operational, Message = "StartMoveFiles",
            Task = Tasks.ConsolidateFiles, Opcode = EventOpcode.Start)]
        public void StartMoveFiles()
        {
            WriteEvent(103);
        }

        [Event(104, Level = EventLevel.Verbose, Channel = EventChannel.Operational,
            Message = "StopMoveFiles \nFiles:{0}", Task = Tasks.ConsolidateFiles, Opcode = EventOpcode.Stop)]
        public void StopMoveFiles(int fileCount)
        {
            WriteEvent(104, fileCount);
        }

        [Event(105, Level = EventLevel.Verbose, Channel = EventChannel.Operational,
            Message = "StartMeasureAndSaveTraces", Task = Tasks.MeasureTrace, Opcode = EventOpcode.Start)]
        public void StartMeasureAndSaveTraces()
        {
            WriteEvent(105);
        }

        [Event(106, Level = EventLevel.Verbose, Channel = EventChannel.Operational,
            Message = "StopMeasureAndSaveTraces \nTraces:{0}", Task = Tasks.MeasureTrace, Opcode = EventOpcode.Stop)]
        public void StopMeasureAndSaveTraces(int traceCount)
        {
            WriteEvent(106, traceCount);
        }

        [Event(107, Level = EventLevel.Informational, Channel = EventChannel.Operational,
            Message = "StartProcessEndToEnd \nWind down time begins in {0} minutes",
            Task = Tasks.ProcessEndToEnd, Opcode = EventOpcode.Start)]
        public void StartProcessEndToEnd(double windDownMinutesFromNow)
        {
            WriteEvent(107, windDownMinutesFromNow);
        }

        [Event(1070, Level = EventLevel.Verbose, Channel = EventChannel.Operational,
            Message = "InProgressProcessEndToEnd status message: \n{0}",
            Task = Tasks.ProcessEndToEnd, Opcode = EventOpcode.Info)]
        public void InProgressProcessEndToEnd(string message)
        {
            WriteEvent(1070, message);
        }

        [Event(1071, Level = EventLevel.Warning, Channel = EventChannel.Operational,
            Message =
                "Unable to process discovered MeasuredTrace {0}. \nOccasional unreadable traces are unavoidable, but if many of your traces are failing to process you should investigate. \nError detail: {1}",
            Task = Tasks.TraceAnalyzeFailureDuringProcessEndToEnd)]
        public void TraceAnalyzeFailureDuringProcessEndToEnd(string path, string errorDetailDump)
        {
            WriteEvent(1071, path, errorDetailDump);
        }

        [Event(1072, Level = EventLevel.Warning, Channel = EventChannel.Operational,
            Message = "Processing task {0} aborted with information:\n{1}",
            Task = Tasks.ProcessingTaskAborted)]
        public void ProcessingTaskAbortedWarning(string task, string detailMessage)
        {
            WriteEvent(1072, task, detailMessage);
        }

        [Event(1073, Level = EventLevel.Verbose, Channel = EventChannel.Operational,
            Message = "StartMeasureAndSaveItem: {0}",
            Task = Tasks.MeasureAndSaveItem, Opcode = EventOpcode.Start)]
        public void StartMeasureAndSaveItem(string tracePath)
        {
            WriteEvent(1073, tracePath);
        }

        [Event(1074, Level = EventLevel.Verbose, Channel = EventChannel.Operational,
            Message = "StopMeasureAndSaveItem: {0}\nDatabase rows inserted: {1}",
            Task = Tasks.MeasureAndSaveItem, Opcode = EventOpcode.Stop)]
        public void StopMeasureAndSaveItem(string tracePath, int databaseRowsCreated)
        {
            WriteEvent(1074, tracePath, databaseRowsCreated);
        }

        [Event(108, Level = EventLevel.Informational, Channel = EventChannel.Operational,
            Message = "StopProcessEndToEnd \nMoved:{0}, \nMeasuredAndSaved:{1}", Task = Tasks.ProcessEndToEnd,
            Opcode = EventOpcode.Stop)]
        public void StopProcessEndToEnd(int movedCount, int processedAndSavedCount)
        {
            WriteEvent(108, movedCount, processedAndSavedCount);
        }
    }
}