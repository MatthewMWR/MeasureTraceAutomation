// Copyright and license at: https://github.com/MatthewMWR/MeasureTraceAutomation/blob/master/LICENSE
using System;
using System.IO;
using System.Linq;
using System.Net;
using Xunit;
using MeasureTraceAutomation;
using MeasureTrace.TraceModel;
using Microsoft.Data.Entity;

namespace MeasureTraceAutomationTests05
{
    public class SmokeTests
    {
        [Fact]
        public void SimpleStoreTest()
        {
            var config = new MeasurementStoreConfig()
            {
                StoreType = StoreType.MicrosoftSqlServer,
                ConnectionString = @"server=(localdb)\MSSqlLocalDb;Database=SimpleStoreTest"
            };
            var trace = new MeasuredTrace() { PackageFileName = "xyz" };
            var measurement = new CpuSampled() { ProcessName = "Foo", IsDpc = true, Count = 100, TotalSamplesDuringInterval = 1000, CpuCoreCount = 1 };
            var measurement2 = new TraceAttribute() { Name = "FooA" };
            trace.AddMeasurement(measurement);
            trace.AddMeasurement(measurement2);
            using (var store = new MeasurementStore(config))
            {
                store.Database.EnsureCreated();
                store.Database.EnsureDeleted();
                store.Database.EnsureCreated();
                Assert.True(store.SaveTraceAndMeasurements(trace) == 3);
            }
        }

        [Fact]
        public void SimpleProcessingTest()
        {
            var dataSource =
                @"https://github.com/MatthewMWR/WinPerf/blob/master/Scenarios/BOOT-REFERENCE__NormalLightlyManaged.zip?raw=true";
            var dataIncomingDir = Path.Combine(Path.GetTempPath(), "SimpleProcessingTest-In");
            Directory.CreateDirectory(dataIncomingDir);
            var dataArchiveDir = Path.Combine(Path.GetTempPath(), "SimpleProcessingTest-Archive");
            Directory.CreateDirectory(dataArchiveDir);
            var webClient = new WebClient();
            webClient.DownloadFile(dataSource, Path.Combine(dataIncomingDir, "Original.zip"));
            var testCopyCount = 30;
            var i = 0;
            while (i < testCopyCount)
            {
                i++;
                File.Copy(Path.Combine(dataIncomingDir, "Original.zip"), Path.Combine(dataIncomingDir, $"Copy{i}.zip"), true);
            }
            var storeConfig = new MeasurementStoreConfig()
            {
                StoreType = StoreType.MicrosoftSqlServer,
                ConnectionString = @"server=(localdb)\MSSqlLocalDb;Database=SimpleProcessingTest"
            };
            var processingConfig = new ProcessingConfig()
            {
                DestinationDataPath = dataArchiveDir
            };
            processingConfig.IncomingDataPaths.Add(dataIncomingDir);
            using (var store = new MeasurementStore(storeConfig))
            {
                store.Database.EnsureCreated();
                store.Database.EnsureDeleted();
                store.Database.EnsureCreated();
            }
            DoWork.InvokeProcessingOnce(processingConfig, storeConfig);
            using (var store = new MeasurementStore(storeConfig))
            {
                Assert.True(store.Traces.Count() == testCopyCount + 1);
                var measuredCount = store.Traces
                    .Include(t => t.ProcessingRecords)
                    .Count(t => t.ProcessingRecords.OrderBy(pr=>pr.StateChangeTime).Last().ProcessingState == ProcessingState.Measured);
                Assert.True( measuredCount == processingConfig.ParallelMeasuringThrottle);
                var movedCount = store.ProcessingRecords.Count(pr => pr.ProcessingState == ProcessingState.Moved);
                Assert.True(movedCount == processingConfig.ParallelMovesThrottle);
            }
            DoWork.InvokeProcessingOnce(processingConfig, storeConfig);
            DoWork.InvokeProcessingOnce(processingConfig, storeConfig);
            DoWork.InvokeProcessingOnce(processingConfig, storeConfig);
            using (var store = new MeasurementStore(storeConfig))
            {
                var measuredCount = store.Traces
                    .Include(t => t.ProcessingRecords)
                    .Count(t => t.ProcessingRecords.OrderBy(pr => pr.StateChangeTime).Last().ProcessingState == ProcessingState.Measured);
                Assert.True(measuredCount == testCopyCount + 1);
            }

        }
    }
}