// Copyright and license at: https://github.com/MatthewMWR/MeasureTraceAutomation/blob/master/LICENSE
namespace MeasureTraceAutomation.Logging
{
    public static class SimpleDebugLog
    {
        public static void LogThis(string message)
        {
            LogImplementation(message);
        }

        private static void LogImplementation(string message)
        {
            RichLog.Log.DebugMessageToOperationalChannel(message);
            //RichLog.Log.DebugMessage(message);
        }
    }
}