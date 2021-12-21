﻿using HunterPie.Core.Architecture;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace HunterPie.UI.Overlay.Widgets.Metrics.ViewModel
{
    public class TelemetricsViewModel : Bindable
    {

        private long _memoryUsage;
        private float _cpuUsage;
        private int _threads;

        private readonly PerformanceCounter _cpuPerfCounter;
        private float _cpuUsageT2;
        private float _cpuUsageT1;

        public long Memory
        {
            get => _memoryUsage;
            set { SetValue(ref _memoryUsage, value); }
        }

        public float CPU
        {
            get => _cpuUsage;
            set { SetValue(ref _cpuUsage, value); }
        }

        public int Threads
        {
            get => _threads;
            set { SetValue(ref _threads, value); }
        }

        public TelemetricsViewModel()
        {
            using (Process self = Process.GetCurrentProcess())
            {
                _cpuPerfCounter = new("Process", "% Processor Time", self.ProcessName);
                _cpuUsageT1 = _cpuPerfCounter.NextValue();
            }
            
            var dispatcher = new DispatcherTimer(DispatcherPriority.Render);
            dispatcher.Tick += new EventHandler(UpdateInformation);
            dispatcher.Interval = new TimeSpan(0, 0, 5);
            dispatcher.Start();
        }

        public void UpdateInformation(object sender, EventArgs e)
        {
            using (Process self = Process.GetCurrentProcess())
            {
                Memory = self.WorkingSet64 / 1024 / 1024;
                Threads = self.Threads.Count;
            }

            CPU = _cpuPerfCounter.NextValue();
        }
    }
}
