// Copyright and license at: https://github.com/MatthewMWR/MeasureTraceAutomation/blob/master/LICENSE
using System.Collections.Generic;
using MeasureTrace.TraceModel;

namespace MeasureTraceAutomation
{
    public class MeasuredTrace : Trace
    {
        public ICollection<ProcessingRecord> ProcessingRecords { get; set; }

        //public override bool Equals(object obj)
        //{
        //    var incoming = obj as MeasuredTrace;
        //    var strongThis = this as MeasuredTrace;
        //    if (incoming == null || strongThis == null) return false;
        //    return string.Equals(incoming.PackageFileName, strongThis.PackageFileName,
        //        StringComparison.OrdinalIgnoreCase);
        //}
        //public override int GetHashCode()
        //{
        //    return this.PackageFileName.ToUpperInvariant().GetHashCode();
        //}
        //public static bool operator ==(MeasuredTrace x, MeasuredTrace y)
        //{
        //    if (x == null || y == null) return false;
        //    return x.Equals(y);
        //}
        //public static bool operator !=(MeasuredTrace x, MeasuredTrace y)
        //{
        //    if (x == null || y == null) return true;
        //    return !x.Equals(y);
        //}
    }
}