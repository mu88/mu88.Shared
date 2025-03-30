# mu88.Shared
![Combine CI / Release](https://github.com/mu88/mu88.Shared/actions/workflows/CI_CD.yml/badge.svg)
[![NuGet version](https://img.shields.io/nuget/v/mu88.Shared)](https://www.nuget.org/packages/mu88.Shared/)
[![NuGet downloads](https://img.shields.io/nuget/dt/mu88.Shared)](https://www.nuget.org/packages/mu88.Shared/)  
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=mu88_mu88.Shared&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=mu88_mu88.Shared)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=mu88_mu88.Shared&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=mu88_mu88.Shared)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=mu88_mu88.Shared&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=mu88_mu88.Shared)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=mu88_mu88.Shared&metric=bugs)](https://sonarcloud.io/summary/new_code?id=mu88_mu88.Shared)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=mu88_mu88.Shared&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=mu88_mu88.Shared)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=mu88_mu88.Shared&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=mu88_mu88.Shared)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=mu88_mu88.Shared&metric=coverage)](https://sonarcloud.io/summary/new_code?id=mu88_mu88.Shared)

## General
This repo contains the code of the NuGet package [`mu88.Shared`](https://www.nuget.org/packages/mu88.Shared/), providing the following features:
- Add and configure certain OpenTelemetry features (metrics and traces)
- Provide a minimalistic health check tool for .NET apps

I use this NuGet package to share features and configurations between different .NET apps, hence avoiding the need to implement it repeatedly.

## Functionality details
### OpenTelemetry
By calling the extension method `ConfigureOpenTelemetry` on an instance of `Microsoft.Extensions.Hosting.IHostApplicationBuilder`, the following OpenTelemetry features will be enabled:
- Metrics
  - ASP.NET Core (e.g. request duration) → [see here](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNetCore#metrics)
  - .NET process information (e.g. process memory) → [see here](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Process#metrics)
  - .NET runtime information (e.g. GC heap size) → [see here](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Runtime#metrics)
- Tracing
  - ASP.NET Core → [see here](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNetCore#traces)
  - Entity Framework Core → [see here](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.EntityFrameworkCore)

To export these data, the .NET configuration parameter `OTEL_EXPORTER_OTLP_ENDPOINT` for the OpenTelemetry endpoint receiving the exported metrics and traces must be configured, e.g. via an environment variable. [See the official OpenTelemetry docs for more information](https://opentelemetry.io/docs/languages/sdk-configuration/otlp-exporter/#otel_exporter_otlp_endpoint).

### Health check tool
As the [GitHub issue "Consider defining a helper "health check" utility for distroless scenarios"](https://github.com/dotnet/dotnet-docker/issues/4300) describes, it is not possible to use the `HEALTHCHECK` instruction in a Docker scenario when using a distroless image as there is neither a shell nor a tool like `curl`. To overcome this limitation, I created a minimalistic health check tool that can be used in .NET apps (inspired by [this comment](https://github.com/dotnet/dotnet-docker/issues/4300#issuecomment-2546036016)).

The tool is a simple `HttpClient` that sends a `GET` request to a specified URL and checks if the response status code is `200 OK` and the response body contains a specified string (`Healthy`). If the check fails, the tool will exit with a non-zero exit code, which can be used in a Dockerfile's `HEALTHCHECK` instruction.

To successfully run this tool it in a Docker container, .NET requires it to provide a `runtimeconfig.json` file for the tool beside the executable. This file is included in the NuGet package as a content file and will be copied to the output directory of the consuming .NET project when building it. Thereby, it will be embedded in the Docker image and the .NET runtime can successfully run the tool.

NuGet recommends to not ship multiple executables in a single NuGet package. However, shipping the tool as a separate NuGet package would require the consuming .NET project to reference two packages, which I wanted to avoid. Therefore, I decided to include the tool in this package. Unfortunately, some ugly MSBuild logic is required to copy all the necessary bits and pieces to the output directory of the consuming .NET project. If you have a better idea on how to solve this, please let me know!

#### Usage
To use the health check tool in a `docker-compose.yml` file, you can add the following service definition:

```yaml
services:
  yourservice:
    container_name: yourcontainer
    image: yourapp:latest
    healthcheck:
      test: [ "CMD", "dotnet", "/app/mu88.HealthCheck.dll", "http://localhost:8080/healthz" ]
      interval: 60s
      timeout: 5s
      retries: 3
      start_period: 30s
      start_interval: 5s
```

As you can see, the health check tool is called with the URL of the health check endpoint of your app and instead of using `bash` or `curl`, the .NET runtime itself is used to run the health check tool. If the health check fails, the Docker container will be restarted after the specified number of retries.
