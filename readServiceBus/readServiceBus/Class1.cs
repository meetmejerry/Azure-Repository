using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Text;

namespace readServiceBus
{
    public static class TelemetryFactory
    {
        private static TelemetryClient _telemetryClient;

        public static TelemetryClient GetTelemetryClient()
        {
            if (_telemetryClient == null)
            {
                string instrumentationKey = "key";
                var telemetryConfiguration = new TelemetryConfiguration { InstrumentationKey = instrumentationKey };
                _telemetryClient = new TelemetryClient(telemetryConfiguration);
            }

            return _telemetryClient;
        }
    }
}
