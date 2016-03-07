// Copyright and license at: https://github.com/MatthewMWR/MeasureTraceAutomation/blob/master/LICENSE
using System;
using System.Collections.Generic;

namespace MeasureTraceAutomation
{
    public class ProcessingConfig
    {
        public ProcessingConfig()
        {
            IncomingDataPaths = new List<string>();
            IncomingFilePatterns = new List<string> {"*.zip", "*.etl"};
            DestinationSubDirPattern = "yyyy-MM";
            ParallelMovesThrottle = Environment.ProcessorCount*6;
            ParallelMeasuringThrottle = Environment.ProcessorCount*4;
        }

        public ICollection<string> IncomingDataPaths { get; }
        public string DestinationDataPath { get; set; }
        public ICollection<string> IncomingFilePatterns { get; }
        public string DestinationSubDirPattern { get; set; }
        public int ParallelMovesThrottle { get; set; }
        public int ParallelMeasuringThrottle { get; set; }
    }
}