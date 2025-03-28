using Microsoft.AspNetCore.Http;
using Prometheus.Client;
using Prometheus.Client.Collectors;
using Prometheus.Client.MetricPusher;
using System;
using WPS_worder_node_1.Modal;

namespace WPS_worder_node_1.BL
{
    public class MyMatricsPusher
    {
        // Static registry & pusher (prevents memory leak)
        private static readonly CollectorRegistry registry = new CollectorRegistry();
        private static readonly MetricFactory factory = new MetricFactory(registry);
        private static readonly IGauge jobExecutionDuration = factory.CreateGauge(
            "http_Gauge_response_time_custom", "Response time in seconds"
        );
        private static readonly IMetricFamily<ICounter, ValueTuple<String>> jobExecutionStatus = factory.CreateCounter(
            "http_status_code_custom", "Count of HTTP status codes", "status_code"
        );

        public static void PushMetrics(string Clinet_id, string Server_id, HealthCheckerModal healthCheckModal)
        {
            // Move pusher here (after defining registry & metrics)
            MetricPusher pusher = new MetricPusher(new MetricPusherOptions
            {
                Endpoint = "http://localhost:9093",
                Job = $"{Clinet_id}",
                Instance = $"{Server_id}",
                CollectorRegistry = registry
            });

            // Push collected metrics
            jobExecutionDuration.Set(healthCheckModal.ResponseTime);
            jobExecutionStatus.WithLabels(healthCheckModal.StatusCode.ToString()).Inc();

            //PushAsync to make current thread available for other task
            //insted of waiting for data push completion
            pusher.PushAsync().GetAwaiter();
            Console.WriteLine("Metrics pushed");
        }
    }
}




