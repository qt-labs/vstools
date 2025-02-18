// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace QtVsTools.Core
{
    using Options;

    public static class Telemetry
    {
        private static readonly string ConnectionString =
            "InstrumentationKey=f651d90d-e953-4d02-af3d-5b4c25aea917;" +
            "IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;" +
            "LiveEndpoint=https://westeurope.livediagnostics.monitor.azure.com/;" +
            "ApplicationId=010a63cc-adf3-4a89-91ce-6ca3c804fad0";

        private static TelemetryClient _telemetryClient = null;

        private static void Initialize()
        {
            var configuration = TelemetryConfiguration.CreateDefault();
            configuration.ConnectionString = ConnectionString;
            _telemetryClient = new TelemetryClient(configuration);
        }

        public static void TrackEvent(string name,
            IDictionary<string, string> properties = null,
            IDictionary<string, double> metrics = null)
        {
            if (QtOptionsPage.TelemetryEnable) {
                if (_telemetryClient == null)
                    Initialize();
                _telemetryClient.TrackEvent(name, properties, metrics);
            }
        }

        public static void TrackException(Exception exception,
            IDictionary<string, string> properties = null,
            IDictionary<string, double> metrics = null)
        {
            if (QtOptionsPage.TelemetryEnable) {
                if (_telemetryClient == null)
                    Initialize();
                _telemetryClient.TrackException(exception, properties, metrics);
            }
        }

        public static void Flush()
        {
            if (_telemetryClient != null)
                _telemetryClient.Flush();
        }
    }
}
