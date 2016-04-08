// Copyright and license at: https://github.com/MatthewMWR/MeasureTraceAutomation/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using MeasureTrace.TraceModel;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;

namespace MeasureTraceAutomation
{
    public static class MeasurementStoreExtension
    {
        public static void HydrateTraceMeasurements(this MeasurementStore store, MeasuredTrace measuredTrace)
        {
            // TODO Go back to dynamically discovering types rather than using hard coded list. For now this works better with EF
            // TODO Move filtering to db engine rather than client side. This is super inneficient, but working around EF
            foreach (var m in store.Set<CpuSampled>().Include(m => m.Trace).Where(m => string.Equals(m.Trace.PackageFileName, measuredTrace.PackageFileName, StringComparison.OrdinalIgnoreCase)))
            {
                measuredTrace.AddMeasurement(m);
            }
            foreach (var m in store.Set<BootPhase>().Include(m => m.Trace).Where(m => string.Equals(m.Trace.PackageFileName, measuredTrace.PackageFileName, StringComparison.OrdinalIgnoreCase)))
            {
                measuredTrace.AddMeasurement(m);
            }
            foreach (var m in store.Set<WinlogonSubscriberTask>().Include(m => m.Trace).Where(m => string.Equals(m.Trace.PackageFileName, measuredTrace.PackageFileName, StringComparison.OrdinalIgnoreCase)))
            {
                measuredTrace.AddMeasurement(m);
            }
            foreach (var m in store.Set<DiskIo>().Include(m => m.Trace).Where(m => string.Equals(m.Trace.PackageFileName, measuredTrace.PackageFileName, StringComparison.OrdinalIgnoreCase)))
            {
                measuredTrace.AddMeasurement(m);
            }
            foreach (var m in store.Set<LogicalDisk>().Include(m => m.Trace).Where(m => string.Equals(m.Trace.PackageFileName, measuredTrace.PackageFileName, StringComparison.OrdinalIgnoreCase)))
            {
                measuredTrace.AddMeasurement(m);
            }
            foreach (var m in store.Set<PhysicalDisk>().Include(m => m.Trace).Where(m => string.Equals(m.Trace.PackageFileName, measuredTrace.PackageFileName, StringComparison.OrdinalIgnoreCase)))
            {
                measuredTrace.AddMeasurement(m);
            }
            foreach (var m in store.Set<TraceAttribute>().Include(m => m.Trace).Where(m => string.Equals(m.Trace.PackageFileName, measuredTrace.PackageFileName, StringComparison.OrdinalIgnoreCase)))
            {
                measuredTrace.AddMeasurement(m);
            }
        }

        public static void HydrateTraceMeasurements<TMeasurement>(this MeasurementStore store,
            MeasuredTrace measuredTrace) where TMeasurement : class, IMeasurement
        {
            foreach (var mm in store.Set<TMeasurement>().Include(m => m.Trace).Where(m => m.Trace == measuredTrace))
            {
                measuredTrace.AddMeasurement(mm);
            }
        }

        public static MeasuredTrace GetTraceWithRecordsFromSparseTrace(this MeasurementStore store, MeasuredTrace measuredTrace)
        {
            return
                store.Traces.Include(t => t.ProcessingRecords)
                    .Where(t => t.IsSameDataPackatge(measuredTrace))
                    .SingleOrDefault();
        }

        public static DbSet<TEntity> NewDynamicSet<TEntity>(this MeasurementStore store, TEntity entity)
            where TEntity : class, IMeasurement
        {
            return store.Set<TEntity>();
        }

        public static IEnumerable<MeasuredTrace> GetTraceByState(
            this MeasurementStore store, ProcessingState processingState)
        {
            // TODO This is super expensive right now because we are doing most of the filtering on the caller side
            // after returing lots of excess data from the db. Unfortunately this was the only ready workaround
            // to an EF bug in query compilation
            foreach (var trace in store.Traces.Include(t=>t.ProcessingRecords))
            {
                if(trace.ProcessingRecords.Latest().ProcessingState == processingState) yield return trace;
            }
        }

        public static MeasuredTrace GetTraceByName(
            this MeasurementStore store, string packageFileName, bool includeMeasurements = false)
        {
            var targetTrace = store.Traces.Include(t => t.ProcessingRecords)
                .Where(t => string.Equals(t.PackageFileName, packageFileName, StringComparison.OrdinalIgnoreCase))
                .SingleOrDefault();
            if(includeMeasurements) store.HydrateTraceMeasurements(targetTrace);
            return targetTrace;
        }

        public static IEnumerable<MeasuredTrace> GetTraceByFilter(
            this MeasurementStore store, Func<MeasuredTrace,bool> filter, bool includeMeasurements = false)
        {
            foreach (var trace in store.Traces.Include(t => t.ProcessingRecords).Where(t => filter(t)))
            {
                if (includeMeasurements) store.HydrateTraceMeasurements(trace);
                yield return trace;
            }
        }

        public static ProcessingRecord Latest(this IEnumerable<ProcessingRecord> records)
        {
            return records.OrderBy(r => r.StateChangeTime).Last();
        }
    }
}