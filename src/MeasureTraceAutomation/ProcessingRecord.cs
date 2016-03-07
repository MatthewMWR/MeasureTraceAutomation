// Copyright and license at: https://github.com/MatthewMWR/MeasureTraceAutomation/blob/master/LICENSE
using System;
using MeasureTrace.TraceModel;

namespace MeasureTraceAutomation
{
    public class ProcessingRecord
    {
        public int Id { get; set; }
        public Trace Trace { get; set; }
        public int TraceId { get; set; }
        public ProcessingState ProcessingState { get; set; }
        public DateTime DiscoverTimeUtc { get; set; }
        public string DiscoveredAtPath { get; set; }
    }
}