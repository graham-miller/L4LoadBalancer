namespace L4LoadBalancer.App.Core;

public class HealthMonitorOptions
{
    public TimeSpan CheckInterval { get; set; }
    public TimeSpan Timeout { get; set; }
}
