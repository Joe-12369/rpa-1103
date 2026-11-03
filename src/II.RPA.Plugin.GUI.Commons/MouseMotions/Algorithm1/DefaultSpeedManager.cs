using System;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    internal class DefaultSpeedManager : ISpeedManager
    {
        private static readonly double SMALL_DELTA = 1E-05;
        private readonly List<Flow> _flows;
        private long _mouseMovementTimeMs = 500;

        public long MouseMovementTimeMs
        {
            get => _mouseMovementTimeMs;
            set => _mouseMovementTimeMs = value;
        }

        public DefaultSpeedManager() => _flows = new List<Flow>()
        {
            new Flow(FlowTemplates.ConstantSpeed()),
            new Flow(FlowTemplates.VariatingFlow()),
            new Flow(FlowTemplates.InterruptedFlow()),
            new Flow(FlowTemplates.InterruptedFlow2()),
            new Flow(FlowTemplates.SlowStartupFlow()),
            new Flow(FlowTemplates.SlowStartup2Flow()),
            new Flow(FlowTemplates.AdjustingFlow()),
            new Flow(FlowTemplates.JaggedFlow()),
            new Flow(FlowTemplates.StoppingFlow())
        };

        public DefaultSpeedManager(IEnumerable<Flow> flows)
        {
            _flows = new List<Flow>();
            _flows.AddRange(flows);
        }

        public void GetFlowWithTime(double distance, out Flow flow, out long time)
        {
            time = _mouseMovementTimeMs + (long)(new Random().NextDouble() * _mouseMovementTimeMs);
            flow = _flows[(int)(new Random().NextDouble() * _flows.Count)];
            double num = time / (double)flow.GetFlowCharacteristics().Length;

            foreach (double flowCharacteristic in flow.GetFlowCharacteristics())
            {
                if (Math.Abs(flowCharacteristic - 0.0) < SMALL_DELTA)
                {
                    time += (long)num;
                }
            }
        }
    }
}
