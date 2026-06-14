# VehicleLink Gateway
Real time V2X communication gateway MQTT over mTLS → Kafka → SignalR → HTML Dashboard. Built with ASP.NET Core 10.0
## Overview
VehicleLink Gateway is a real time Vehicle-to-Everything (V2X) communication 
gateway built with ASP.NET Core 10.0. It ingests telemetry from vehicles and 
roadside units over MQTT with mutual TLS authentication, streams events through 
Apache Kafka, and broadcasts safety critical alerts to a live HTML operator 
dashboard via SignalR WebSocket all deployable on Docker and Azure Kubernetes 
Service (AKS).

The project addresses core challenges in modern V2X infrastructure:
- **Secure device identity**  only certificate-verified devices can publish (mTLS)
- **High-throughput ingestion**  decoupled event pipeline via Kafka
- **Real-time operator visibility**  sub second push to connected clients via SignalR
- **Cloud-native scale**  containerised with Docker, deployable to AKS with 2+ replicas

## Tech Stack

| Layer | Technology |
|---|---|
| Gateway API | ASP.NET Core 10.0, C# |
| Secure messaging | MQTT (MQTTnet), mTLS, OpenSSL |
| Event streaming | Apache Kafka (Confluent.Kafka) |
| Real-time push | SignalR |
| Persistence | EF Core, SQL Server |
| Frontend | HTML, @microsoft/signalr |
| Deployment | Docker, Azure Kubernetes Service (AKS) |

## Architecture

```mermaid
flowchart TD
    A["🚗 Vehicle / RSU Simulator\nConsole App · MQTTnet"]
    B["🔒 Mosquitto Broker\nport 8883 · mTLS"]
    C["⚙️ ASP.NET Core Gateway API\n.NET 10 · BackgroundService"]
    D["📨 Kafka Producer\nConfluent.Kafka"]
    E["📬 Kafka Topic\nvehicle-telemetry"]
    F["📥 Kafka Consumer\nBackgroundService"]
    G["📡 SignalR Hub\nTelemetryHub"]
    H["🖥️ HTML Dashboard\nlive map · alert feed"]
    I["🗄️ SQL Server\nEF Core · telemetry logs"]
    J["☁️ Docker + AKS\nAzure Kubernetes Service"]

    A -->|"MQTT over mTLS"| B
    B -->|"verified device identity"| C
    C --> D
    D --> E
    E --> F
    F -->|"push telemetry"| G
    G -->|"WebSocket"| H
    C -->|"persist events"| I
    C -.->|"deployed on"| J

    style A fill:#0a3d2e,stroke:#1d9e75,color:#3dcfa0
    style B fill:#0a3d2e,stroke:#1d9e75,color:#3dcfa0
    style C fill:#0c2d4a,stroke:#378add,color:#7bbfef
    style D fill:#1e1a4a,stroke:#7f77dd,color:#b0aaee
    style E fill:#1e1a4a,stroke:#7f77dd,color:#b0aaee
    style F fill:#1e1a4a,stroke:#7f77dd,color:#b0aaee
    style G fill:#1e1a4a,stroke:#7f77dd,color:#b0aaee
    style H fill:#0a3d2e,stroke:#1d9e75,color:#3dcfa0
    style I fill:#3d2a0a,stroke:#e3a034,color:#f0c070
    style J fill:#3d1010,stroke:#e24b4a,color:#f07070
```
## Live Demo

<img width="800" height="404" alt="Image" src="https://github.com/user-attachments/assets/fe698a22-5200-4d26-b4a1-d64d893586a7"/>

> Real-time V2X telemetry from 3 vehicles flowing through 
> MQTT over mTLS → Kafka → SignalR → operator dashboard.
> EMERGENCY events trigger live alerts with visual indicators.
## How to Run

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Git Bash](https://git-scm.com/downloads) (for certificate generation on Windows)

---

### 1 — Clone the repo

```bash
git clone https://github.com/Md-Junaeid-Alam/VehicleLink-Gateway.git
cd VehicleLink-Gateway
```

---

### 2 — Generate certificates

Open **Git Bash** in the project root:

```bash
mkdir certs && cd certs

# CA
openssl genrsa -out ca.key 2048
openssl req -new -x509 -days 365 -key ca.key -out ca.crt -subj "//CN=VehicleLinkCA"

# Broker
openssl genrsa -out server.key 2048
openssl req -new -key server.key -out server.csr -subj "//CN=mqtt-broker"
openssl x509 -req -in server.csr -CA ca.crt -CAkey ca.key -CAcreateserial -out server.crt -days 365

# Vehicle simulator
openssl genrsa -out client.key 2048
openssl req -new -key client.key -out client.csr -subj "//CN=vehicle-001"
openssl x509 -req -in client.csr -CA ca.crt -CAkey ca.key -CAcreateserial -out client.crt -days 365

# Gateway
openssl genrsa -out gateway.key 2048
openssl req -new -key gateway.key -out gateway.csr -subj "//CN=gateway"
openssl x509 -req -in gateway.csr -CA ca.crt -CAkey ca.key -CAcreateserial -out gateway.crt -days 365

cd ..
```

> **Windows:** Use `//CN=` (double slash) in Git Bash. On Linux/Mac use `/CN=`.

---

### 3 — Start the Docker stack

```bash
docker compose up -d
docker compose ps   # verify all 4 containers are running
```
| Container | Purpose | Port |
|---|---|---|
| mosquitto | MQTT broker with mTLS | 8883 |
| kafka | Event streaming | 9092 |
| zookeeper | Kafka coordination | 2181 |
| sqlserver | Telemetry persistence | 1433 |

---

### 4 — Run the EF Core migration *(first time only)*

```powershell
dotnet ef migrations add InitialCreate `
  --project VehicleLink.Infrastructure `
  --startup-project VehicleLink.Gateway

dotnet ef database update `
  --project VehicleLink.Infrastructure `
  --startup-project VehicleLink.Gateway
```

---

### 5 — Run the Gateway API

```powershell
cd VehicleLink.Gateway
dotnet run
```
wait for: info: MQTT connected listening on v2x/telemetry/#
info: Now listening on: http://localhost:5038

---

### 6 — Run the Vehicle Simulator

Open a **second terminal**:

```powershell
cd VehicleLink.Simulator
dotnet run
```

3 vehicles start publishing live telemetry every second.

---

### 7 — Open the dashboard

Live map, scrolling telemetry feed, and real-time EMERGENCY alerts.

---

### Stop everything

```bash
docker compose down      # keep data
docker compose down -v   # wipe all data
```

---

