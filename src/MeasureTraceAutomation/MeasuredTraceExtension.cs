// Copyright and license at: https://github.com/MatthewMWR/MeasureTraceAutomation/blob/master/LICENSE
using System;

namespace MeasureTraceAutomation
{
    public static class MeasuredTraceExtension
    {
        public static bool IsSameDataPackatge(this MeasuredTrace traceX, MeasuredTrace traceY)
        {
            if (traceX == null || traceY == null) return false;
            return string.Equals(traceX.PackageFileName, traceY.PackageFileName, StringComparison.OrdinalIgnoreCase);
        }
    }
}