// Copyright and license at: https://github.com/MatthewMWR/MeasureTraceAutomation/blob/master/LICENSE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MeasureTrace;
using MeasureTrace.Adapters;
using MeasureTrace.Calipers;
using MeasureTrace.TraceModel;
using MeasureTraceAutomation.Logging;
using Microsoft.Data.Entity;
using BootPhase = MeasureTrace.Calipers.BootPhase;
using CpuSampled = MeasureTrace.Calipers.CpuSampled;

namespace MeasureTraceAutomation
{
    public static class DoWork
    {
        public static void InvokeProcessingOnce(ProcessingConfig processingConfig, MeasurementStoreConfig storeConfig)
        {
            RichLog.Log.StartProcessEndToEnd(0);
            DiscoverOneBatch(processingConfig, storeConfig);
            var moved = MoveOneBatch(processingConfig, storeConfig);
            var measured = MeasureOneBatch(processingConfig, storeConfig);
            //PostMeasureActionOneBatch(storeConfig, PostMeasureAction);
            RichLog.Log.StopProcessEndToEnd(moved, measured);
        }

        internal static int DiscoverOneBatch(ProcessingConfig processingConfig, MeasurementStoreConfig storeConfig)
        {
            var totalDiscoveries = 0;
            using (var store = new MeasurementStore(storeConfig))
            {
                foreach (var incomingDataPath in processingConfig.IncomingDataPaths)
                {
                    var discoveredCount = 0;
                    RichLog.Log.StartDiscoverFiles(incomingDataPath);
                    foreach (var filter in processingConfig.IncomingFilePatterns)
                    {
                        foreach (
                            var fileSystemEntryPath in
                                Directory.EnumerateFileSystemEntries(incomingDataPath, filter,
                                    SearchOption.AllDirectories))
                        {
                            var fileInfo = new FileInfo(fileSystemEntryPath);
                            if (!store.Traces.Any(t => string.Equals(t.PackageFileName, fileInfo.Name)))
                            {
                                store.Traces.Add(new Trace
                                {
                                    PackageFileName = fileInfo.Name,
                                    PackageFileNameFull = 
                                        CalculateDestinationPath(fileSystemEntryPath, processingConfig, true)
                                });
                                store.SaveChanges();
                            }
                            var trace = store.Traces.First(t => string.Equals(t.PackageFileName, fileInfo.Name));
                            if (
                                !store.ProcessingRecords.Any(
                                    t =>
                                        string.Equals(t.Trace.PackageFileName, trace.PackageFileName,
                                            StringComparison.OrdinalIgnoreCase)))
                            {
                                store.ProcessingRecords.Add(new ProcessingRecord
                                {
                                    Trace = trace,
                                    DiscoverTimeUtc = DateTime.UtcNow,
                                    DiscoveredAtPath = fileSystemEntryPath,
                                    ProcessingState = ProcessingState.Discovered
                                });
                                store.SaveChanges();
                                discoveredCount++;
                                totalDiscoveries++;
                            }
                        }
                    }
                    RichLog.Log.StopDiscoverFiles(incomingDataPath, discoveredCount);
                }
            }
            return totalDiscoveries;
        }

        internal static int MoveOneBatch(ProcessingConfig processingConfig, MeasurementStoreConfig storeConfig)
        {
            RichLog.Log.StartMoveFiles();
            var movedCount = 0;
            var filesToMove = new List<string>(processingConfig.ParallelMovesThrottle);
            var fileMoveTasks = new List<Task>();
            using (var store = new MeasurementStore(storeConfig))
            {
                filesToMove.AddRange(
                    store.ProcessingRecords.Where(pr => pr.ProcessingState == ProcessingState.Discovered)
                        .Take(processingConfig.ParallelMovesThrottle)
                        .Select(pr => pr.DiscoveredAtPath));
            }
            foreach (var fileSourcePath in filesToMove)
            {
                var destinationFullPath = CalculateDestinationPath(fileSourcePath, processingConfig, true);
                fileMoveTasks.Add(
                    Task.Run(() =>
                    {
                        if (File.Exists(destinationFullPath)) File.Delete(destinationFullPath);
                        File.Move(fileSourcePath, destinationFullPath);
                        movedCount++;
                        using (var store = new MeasurementStore(storeConfig))
                        {
                            var processingRecord =
                                store.ProcessingRecords.LastOrDefault(
                                    pr =>
                                        string.Equals(pr.Trace.PackageFileNameFull, destinationFullPath,
                                            StringComparison.OrdinalIgnoreCase));
                            if (processingRecord == null ||
                                processingRecord.ProcessingState != ProcessingState.Discovered)
                                throw new ApplicationException(
                                    "Unexpected processing record state. Reference:F61F914A-0401-4E2D-9A25-CFF0B16F24FD");
                            processingRecord.ProcessingState = ProcessingState.Moved;
                            store.SaveChanges();
                        }
                    }));
            }
            Task.WaitAll(fileMoveTasks.ToArray());
            RichLog.Log.StopMoveFiles(movedCount);
            return movedCount;
        }

        internal static int MeasureOneBatch(ProcessingConfig processingConfig, MeasurementStoreConfig storeConfig)
        {
            RichLog.Log.StartMeasureAndSaveTraces();
            var measuredCount = 0;
            var measuringTasks = new List<Task>();
            var measuringResults = new ConcurrentBag<Trace>();

            using (var store = new MeasurementStore(storeConfig))
            {
                foreach (
                    var pr in
                        store.ProcessingRecords.Where(pr => pr.ProcessingState == ProcessingState.Moved)
                            .Take(processingConfig.ParallelMeasuringThrottle)
                            .Include(pr => pr.Trace))
                {
                    RichLog.Log.StartMeasureAndSaveItem(pr.Trace.PackageFileNameFull);
                    measuringTasks.Add(
                        Task.Run(() =>
                        {
                            using (var tj = new TraceJob(pr.Trace))
                            {
                                tj.RegisterCaliperByType<CpuSampled>();
                                tj.RegisterCaliperByType<BootPhase>();
                                tj.RegisterProcessorByType<GroupPolicyActionProcessor>(ProcessorTypeCollisionOption.UseExistingIfFound);
                                var traceOut = tj.Measure();
                                measuringResults.Add(traceOut);
                            }
                            measuredCount++;
                        }));
                }
            }

            Task.WaitAll(measuringTasks.ToArray());
            using (var store = new MeasurementStore(storeConfig))
            {
                foreach (var t in measuringResults)
                {
                    var addedRows = store.SaveTraceAndMeasurements(t);
                    var processingRecord =
                        store.ProcessingRecords.Last(
                            pr =>
                                string.Equals(pr.Trace.PackageFileName, t.PackageFileName,
                                    StringComparison.OrdinalIgnoreCase));
                    processingRecord.ProcessingState = ProcessingState.Measured;
                    store.SaveChanges();
                    RichLog.Log.StopMeasureAndSaveItem(t.PackageFileNameFull, addedRows);
                }
            }
            RichLog.Log.StopMeasureAndSaveTraces(measuredCount);
            return measuredCount;
        }


        internal static string CalculateDestinationPath(string fileSourcePath, ProcessingConfig processingConfig,
            bool prepareSubDirsAsNeeded)
        {
            var relativeName = new FileInfo(fileSourcePath).Name;
            var destinationRootDir = processingConfig.DestinationDataPath;
            //  TODO FUTURE: Using last modified for now but this is fragile -- switch to package format aware dating
            var fileDate = new FileInfo(fileSourcePath).LastWriteTime;
            var subDirToken = fileDate.ToString(processingConfig.DestinationSubDirPattern);
            var destinationFolderPath = Path.Combine(destinationRootDir, subDirToken);
            if (prepareSubDirsAsNeeded) Directory.CreateDirectory(destinationFolderPath);
            return Path.Combine(destinationFolderPath, relativeName);
        }
    }
}