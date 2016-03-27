// Copyright and license at: https://github.com/MatthewMWR/MeasureTraceAutomation/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MeasureTrace.TraceModel;
using Microsoft.Data.Entity;

namespace MeasureTraceAutomation
{
    public static class MeasurementStoreExtension
    {
        public static string GetDiscoveredAtPath(this MeasurementStore store, MeasuredTrace measuredTrace)
        {
            var dbtrace = store.Traces.FirstOrDefault(t => t == measuredTrace);
            if (dbtrace == null) return string.Empty;
            return
                dbtrace.TraceAttributes.Where(
                    ta =>
                        string.Equals(ta.Name, MeasurementStore.NameOfProcessingStateAttribute,
                            StringComparison.OrdinalIgnoreCase))
                    .Where(ta => ta.WholeNumberValue == (int) ProcessingState.Discovered)
                    .OrderBy(ta => ta.DateTimeValue)
                    .Select(ta => Path.Combine(ta.StringValue, ta.Trace.PackageFileName))
                    .Last();
        }

        public static void HydrateTraceMeasurements(this MeasurementStore store, MeasuredTrace measuredTrace)
        {
            foreach (
                var mm in
                    store.Model.GetEntityTypes()
                        .Where(et => et.ClrType.GetInterfaces().Contains(typeof (IMeasurement)))
                        .Select(mtype => NewDynamicSet(store, (IMeasurement) Activator.CreateInstance(mtype.ClrType)))
                        .SelectMany(set => set.Where(m => m.Trace == measuredTrace)))
            {
                measuredTrace.AddMeasurement(mm);
            }
        }

        public static void HydrateTraceMeasurements<TMeasurement>(this MeasurementStore store,
            MeasuredTrace measuredTrace) where TMeasurement : class, IMeasurement
        {
            foreach (var mm in store.Set<TMeasurement>().Where(m => m.Trace == measuredTrace))
            {
                measuredTrace.AddMeasurement(mm);
            }
        }

        public static DbSet<TEntity> NewDynamicSet<TEntity>(this MeasurementStore store, TEntity entity)
            where TEntity : class, IMeasurement
        {
            return store.Set<TEntity>();
        }

        public static IEnumerable<ProcessingRecord> GetLatestProcessingRecordForTraceByState(
            this MeasurementStore store, ProcessingState processingState)
        {
            return store.ProcessingRecords.GroupBy(pr => pr.MeasuredTrace.PackageFileName)
                .Where(gpr => gpr.AsEnumerable().Latest().ProcessingState == processingState)
                .Select(gpr => gpr.Latest())
                .Include(pr => pr.MeasuredTrace);
        }

        public static ProcessingRecord Latest(this IEnumerable<ProcessingRecord> records)
        {
            return records.OrderBy(r => r.StateChangeTime).Last();
        }
    }
}