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
                var mType in
                    store.Model.GetEntityTypes()
                        .Where(et => et.ClrType.GetInterfaces().Contains(typeof (IMeasurement))))
            {
                var set = NewDynamicSet(store, (IMeasurement)Activator.CreateInstance(mType.ClrType));
                foreach (var m in set.Where(mm => measuredTrace.IsSameDataPackatge(mm.Trace)))
                {
                    measuredTrace.AddMeasurement(m);
                }
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
            return store.ProcessingRecords.Include(pr => pr.MeasuredTrace)
                .GroupBy(pr => pr.MeasuredTrace.PackageFileName)
                .Where(gpr => gpr.AsEnumerable().Latest().ProcessingState == processingState)
                .Select(gpr => gpr.Latest().MeasuredTrace);

        }

        public static ProcessingRecord Latest(this IEnumerable<ProcessingRecord> records)
        {
            return records.OrderBy(r => r.StateChangeTime).Last();
        }
    }
}