﻿// Copyright and license at: https://github.com/MatthewMWR/MeasureTraceAutomation/blob/master/LICENSE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MeasureTrace;
using MeasureTrace.TraceModel;
using MeasureTraceAutomation.Logging;
using Microsoft.Data.Entity;

namespace MeasureTraceAutomation
{
    internal static class AutomationTasks
    {
        public static int DiscoverOneBatch(ProcessingConfig processingConfig, MeasurementStoreConfig storeConfig)
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
                            var sparseTrace = new MeasuredTrace
                            {
                                PackageFileName = fileInfo.Name,
                                PackageFileNameFull =
                                    CalculateDestinationPath(fileSystemEntryPath, processingConfig, true)
                            };
                            if (!store.Traces.Any(t => t.IsSameDataPackage(sparseTrace)))
                            {
                                store.Traces.Add(sparseTrace);
                                store.SaveChanges();
                            }
                            var dbTrace = store.Traces.Include(t => t.ProcessingRecords).First(t => t.IsSameDataPackage(sparseTrace));
                            var lastPr = dbTrace.ProcessingRecords.Latest();
                            if (lastPr == null || lastPr.ProcessingState != ProcessingState.Discovered)
                            {
                                store.Set<ProcessingRecord>().Add(new ProcessingRecord
                                {
                                    MeasuredTrace = dbTrace,
                                    ProcessingState = ProcessingState.Discovered,
                                    StateChangeTime = DateTime.UtcNow,
                                    Path = fileSystemEntryPath
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

        public static int MoveOneBatch(ProcessingConfig processingConfig, MeasurementStoreConfig storeConfig)
        {
            RichLog.Log.StartMoveFiles();
            var movedCount = 0;
            var filesToMove = new List<string>(processingConfig.ParallelMovesThrottle);
            var fileMoveTasks = new List<Task>();
            using (var store = new MeasurementStore(storeConfig))
            {
                var inScopeTraces = store.Traces.Include(t => t.ProcessingRecords)
                    .Where(
                        t =>
                            t.ProcessingRecords.OrderBy(pr => pr.StateChangeTime).Last().ProcessingState ==
                            ProcessingState.Discovered)
                    .Take(processingConfig.ParallelMovesThrottle);
                foreach (var t in inScopeTraces)
                {
                    filesToMove.Add(t.ProcessingRecords.Latest().Path);
                }
            }
            foreach (var fileSourcePath in filesToMove.Where(p => !string.IsNullOrEmpty(p)))
            {
                if (!File.Exists(fileSourcePath)) continue;
                var destinationFullPath = CalculateDestinationPath(fileSourcePath, processingConfig, true);
                fileMoveTasks.Add(
                    Task.Run(() =>
                    {
                        if (File.Exists(destinationFullPath)) File.Delete(destinationFullPath);
                        File.Move(fileSourcePath, destinationFullPath);
                        movedCount++;
                        using (var store = new MeasurementStore(storeConfig))
                        {
                            var trace =
                                store.Traces.Where(
                                    t =>
                                        string.Equals(t.PackageFileNameFull, destinationFullPath,
                                            StringComparison.OrdinalIgnoreCase)).Single();

                            store.ProcessingRecords.Add(new ProcessingRecord
                            {
                                MeasuredTrace = trace,
                                Path = destinationFullPath,
                                StateChangeTime = DateTime.UtcNow,
                                ProcessingState = ProcessingState.Moved
                            });
                            store.SaveChanges();
                        }
                    }));
            }
            Task.WaitAll(fileMoveTasks.ToArray());
            RichLog.Log.StopMoveFiles(movedCount);
            return movedCount;
        }

        public static int MeasureOneBatch(ProcessingConfig processingConfig, MeasurementStoreConfig storeConfig)
        {
            RichLog.Log.StartMeasureAndSaveTraces();
            var measuredCount = 0;
            var measuringTasks = new List<Task>();
            var measuringResults = new ConcurrentBag<Trace>();

            using (var store = new MeasurementStore(storeConfig))
            {
                var inScopeTraces = store.Traces.Include(t => t.ProcessingRecords)
                    .Where(
                        t =>
                            t.ProcessingRecords.OrderBy(pr => pr.StateChangeTime).Last().ProcessingState ==
                            ProcessingState.Moved)
                    .Take(processingConfig.ParallelMeasuringThrottle);
                foreach (var t in inScopeTraces)
                {
                    RichLog.Log.StartMeasureAndSaveItem(t.PackageFileNameFull);
                    measuringTasks.Add(
                        Task.Run(() =>
                        {
                            Trace traceOut = null;
                            try
                            {
                                using (var tj = new TraceJob(t))
                                {
                                    tj.StageForProcessing();
                                    tj.RegisterCalipersAllKnown();
                                    traceOut = tj.Measure();
                                }
                            }
                            finally
                            {
                                measuredCount++;
                                if (traceOut == null) traceOut = t;
                                measuringResults.Add(traceOut);
                            }
                        }));
                }
            }
            try
            {
                Task.WaitAll(measuringTasks.ToArray());
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    RichLog.Log.TraceAnalyzeFailureDuringProcessEndToEnd(e.Message, e.ToString());
                }
            }
            using (var store = new MeasurementStore(storeConfig))
            {
                foreach (var t in measuringResults)
                {
                    var addedRows = store.SaveTraceAndMeasurements((MeasuredTrace) t);
                    store.ProcessingRecords.Add(new ProcessingRecord
                    {
                        MeasuredTrace = (MeasuredTrace) t,
                        StateChangeTime = DateTime.UtcNow,
                        ProcessingState = ProcessingState.Measured,
                        Path = t.PackageFileNameFull
                    });
                    store.SaveChanges();
                    RichLog.Log.StopMeasureAndSaveItem(t.PackageFileNameFull, addedRows);
                }
            }
            RichLog.Log.StopMeasureAndSaveTraces(measuredCount);
            return measuredCount;
        }

        public static string CalculateDestinationPath(string fileSourcePath, ProcessingConfig processingConfig,
            bool prepareSubDirsAsNeeded)
        {
            var relativeName = new FileInfo(fileSourcePath).Name;
            var destinationRootDir = processingConfig.DestinationDataPath;
            var tempTrace = new Trace();
            var packageType = TraceJobExtension.ResolvePackageType(fileSourcePath);
            var adapter = TraceJobExtension.GetPackageAdapter(packageType);
            adapter.PopulateTraceAttributesFromFileName(tempTrace, fileSourcePath);
            var fileDate = tempTrace.TracePackageTime;
            var subDirToken = fileDate.ToString(processingConfig.DestinationSubDirPattern);
            var destinationFolderPath = Path.Combine(destinationRootDir, subDirToken);
            if (prepareSubDirsAsNeeded) Directory.CreateDirectory(destinationFolderPath);
            return Path.Combine(destinationFolderPath, relativeName);
        }
    }
}