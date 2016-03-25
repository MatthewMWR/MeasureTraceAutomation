﻿// Copyright and license at: https://github.com/MatthewMWR/MeasureTraceAutomation/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MeasureTrace.TraceModel;
using Microsoft.Data.Entity;

namespace MeasureTraceAutomation
{
    public class MeasurementStore : DbContext
    {
        private readonly MeasurementStoreConfig _measurementStoreConfig;
        public MeasurementStore(MeasurementStoreConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _measurementStoreConfig = config;
        }

        public DbSet<Trace> Traces { get; set; }
        public DbSet<ProcessingRecord> ProcessingRecords { get; set; }

        public int SaveTraceAndMeasurements(Trace trace)
        {
            var changeCount = 0;
            if (
                Traces.Any(
                    t =>
                        string.Equals(t.DataFileNameRelative, trace.DataFileNameRelative,
                            StringComparison.OrdinalIgnoreCase)))
                Traces.Update(trace);
            else Traces.Add(trace);
            changeCount += SaveChanges();
            foreach (var m in trace.GetMeasurementsAll())
            {
                AddMeasurementByTypeInfer(m);
            }
            changeCount += SaveChanges();
            return changeCount;
        }

        private void AddMeasurementByTypeInfer<TMeasurement>(TMeasurement measurement) where TMeasurement : class
        {
            Add(measurement);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_measurementStoreConfig.StoreType == StoreType.MicrosoftSqlServer)
            {
                optionsBuilder.UseSqlServer(_measurementStoreConfig.ConnectionString);
            }
            else
            {
                throw new NotImplementedException("Only Sql Server is implemented as store type");
            }
            optionsBuilder.EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Trace>(b =>
            {
                b.Ignore(t => t.TraceAttributes);
                b.HasKey(trace => trace.DataFileNameRelative);
            });
            //Add measurement types to model
            var measurementTypes = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
            {
                try
                {
                    measurementTypes.AddRange(assembly.GetExportedTypes()
                        .Where(t => t.GetInterfaces().Contains(typeof (IMeasurement))));
                }
                catch (ReflectionTypeLoadException)
                {
                }
                catch (FileNotFoundException)
                {
                }
            }
            foreach (var mt in measurementTypes)
            {
                modelBuilder.Entity(mt)
                    .Property<string>("DataFileNameRelative")
                    .Metadata.IsShadowProperty = true;
                if (mt == typeof (ProcessingRecord))
                    modelBuilder.Entity(mt)
                        .HasOne(typeof (Trace), "Trace")
                        .WithOne()
                        .HasForeignKey("DataFileNameRelative");
                else
                {
                    modelBuilder.Entity(mt)
                        .HasOne(typeof (Trace), "Trace")
                        .WithMany()
                        .HasForeignKey("DataFileNameRelative");
                }
                modelBuilder.Entity(mt).HasKey("Id");
                modelBuilder.Entity(mt).Property<int>("Id").ValueGeneratedOnAdd();
                foreach (var noSetProperty in mt.GetProperties().Where(pi => pi.SetMethod == null))
                {
                    if (noSetProperty.PropertyType == typeof (string))
                    {
                        modelBuilder.Entity(mt).Property<string>(noSetProperty.Name).Metadata.IsShadowProperty = true;
                    }
                    else if (noSetProperty.PropertyType == typeof (double))
                    {
                        modelBuilder.Entity(mt).Property<double>(noSetProperty.Name).Metadata.IsShadowProperty = true;
                    }
                    else if (noSetProperty.PropertyType == typeof (int))
                    {
                        modelBuilder.Entity(mt).Property<int>(noSetProperty.Name).Metadata.IsShadowProperty = true;
                    }
                    else if (noSetProperty.PropertyType == typeof (bool))
                    {
                        modelBuilder.Entity(mt).Property<bool>(noSetProperty.Name).Metadata.IsShadowProperty = true;
                    }
                    else modelBuilder.Entity(mt).Ignore(noSetProperty.Name);
                }
            }
        }

        public override int SaveChanges()
        {
            foreach (
                var entry in
                    ChangeTracker.Entries().Where(entry => entry.Metadata.GetProperties().Any(p => p.IsShadowProperty)))
            {
                foreach (
                    var shadowProp in
                        entry.Metadata.GetProperties()
                            .Where(p => p.IsShadowProperty && !p.IsPrimaryKey() && !p.IsForeignKey()))
                {
                    entry.Property(shadowProp.Name).CurrentValue =
                        entry.Entity.GetType().GetProperty(shadowProp.Name).GetValue(entry.Entity);
                }
            }
            return base.SaveChanges();
        }
    }
}