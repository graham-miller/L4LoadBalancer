namespace L4LoadBalancer.App.Infrastructure;

public class HealthMonitorOptions
{
    public TimeSpan CheckInterval { get; set; }
    public TimeSpan Timeout { get; set; }
}
