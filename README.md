# L4LoadBalancer

A software-based Layer 4 (TCP) load balancer for distributing traffic across multiple backend services.

## Features

- **Multi-client support**: accepts concurrent TCP connections from multiple clients.
- **Multiple strategies**: round robin and least connections load balancing.
- **Health monitoring**: automatic detection and removal of unhealthy backends.
- **TCP-based**: pure Layer 4 proxying without application-layer concerns.
- **Configurable**: flexible settings via `appsettings.json` and environment variables.

## Getting Started

### Prerequisites
- .NET 10

### Demo with Aspire

Start the full orchestrated environment with 3 test backends:

```
dotnet run --project L4LoadBalancer.AppHost/
```

Open Aspire dashboard at [https://l4loadbalancer.dev.localhost:17263/](https://l4loadbalancer.dev.localhost:17263/).

Alternatively, if you have Aspire CLI installed, you can start the environment with:
```
aspire run
```

- Load balancer listens on port 8080.
- Test backends run on ports 5001, 5002, 5003.
- Use Aspire custom resource commands on `loadbalancer` to send TCP test packets:
  - **Send TCP test packet**: sends single test packet.
  - **Send multiple TCP test packets**: sends multiple packets to demonstrate load balancing.

![Alternative Text](docs/resources/aspire-dashboard.png)

## Configuration

### LoadBalancing Strategy

In `appsettings.json`:

```
{ "LoadBalancer": { "Strategy": "RoundRobin"  // or "LeastConnections" } }
```

- `RoundRobin`: cycles through healthy servers sequentially. Best for uniform workloads.
- `LeastConnections`: routes to the server with fewest active connections. Better for long-lived connections.


### Health Monitoring

```
{ "HealthMonitor": { "CheckInterval": "00:00:05", "Timeout": "00:00:02" } }
```

## Testing

### Unit tests

```
dotnet test tests/L4LoadBalancer.App.UnitTests/
```

### Integration tests

```
dotnet test tests/L4LoadBalancer.App.IntegrationTests/
```

