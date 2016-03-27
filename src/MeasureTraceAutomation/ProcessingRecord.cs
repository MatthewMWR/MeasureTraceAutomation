// Copyright and license at: https://github.com/MatthewMWR/MeasureTraceAutomation/blob/master/LICENSE
using System;

namespace MeasureTraceAutomation
{
    public class ProcessingRecord
    {
        public int Id { get; set; }
        public MeasuredTrace MeasuredTrace { get; set; }
        public ProcessingState ProcessingState { get; set; }
        public DateTime StateChangeTime { get; set; }
        public string Path { get; set; }
    }
}