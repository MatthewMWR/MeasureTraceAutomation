// Copyright and license at: https://github.com/MatthewMWR/MeasureTraceAutomation/blob/master/LICENSE

using MeasureTraceAutomation.Logging;

namespace MeasureTraceAutomation
{
    public static class Automate
    {
        public static void InvokeProcessingOnce(ProcessingConfig processingConfig, MeasurementStoreConfig storeConfig)
        {
            RichLog.Log.StartProcessEndToEnd(0);
            AutomationTasks.DiscoverOneBatch(processingConfig, storeConfig);
            var moved = AutomationTasks.MoveOneBatch(processingConfig, storeConfig);
            var measured = AutomationTasks.MeasureOneBatch(processingConfig, storeConfig);
            RichLog.Log.StopProcessEndToEnd(moved, measured);
        }

    }
}