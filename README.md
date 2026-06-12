# VehicleLink-Gateway
Real-time V2X communication gateway MQTT over mTLS → Kafka → SignalR → Angular Dashboard. Built with ASP.NET Core 10.0
## Overview
VehicleLink Gateway is a real-time Vehicle-to-Everything (V2X) communication 
gateway built with ASP.NET Core 8. It ingests telemetry from vehicles and 
roadside units over MQTT with mutual TLS authentication, streams events through 
Apache Kafka, and broadcasts safety-critical alerts to a live Angular operator 
dashboard via SignalR WebSocket — all deployable on Docker and Azure Kubernetes 
Service (AKS).

The project addresses core challenges in modern V2X infrastructure:
- **Secure device identity** — only certificate-verified devices can publish (mTLS)
- **High-throughput ingestion** — decoupled event pipeline via Kafka
- **Real-time operator visibility** — sub-second push to connected clients via SignalR
- **Cloud-native scale** — containerised with Docker, deployable to AKS with 2+ replicas

## Tech Stack

| Layer | Technology |
|---|---|
| Gateway API | ASP.NET Core 8, C# |
| Secure messaging | MQTT (MQTTnet), mTLS, OpenSSL |
| Event streaming | Apache Kafka (Confluent.Kafka) |
| Real-time push | SignalR |
| Persistence | EF Core, SQL Server |
| Frontend | Angular, @microsoft/signalr |
| Deployment | Docker, Azure Kubernetes Service (AKS) |

## Architecture

[Vehicle / RSU Simulator]
        │  MQTT over mTLS (port 8883)
        ▼
[Mosquitto Broker]  ← device identity verified via client certificates
        │
        ▼
[ASP.NET Core Gateway API]
        ├──► Kafka Producer → [Topic: vehicle-telemetry]
        │                            │
        │                     Kafka Consumer
        │                            │
        └──► SignalR Hub ◄───────────┘
                │
        [Angular Dashboard]  ← live map + alert feed
                │
        [SQL Server / EF Core]  ← persisted telemetry logs
