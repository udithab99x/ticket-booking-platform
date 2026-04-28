# Event Ticket Booking Platform

A cloud-native microservices application for booking event tickets, built for EC7205 Cloud Computing (University of Ruhuna, Semester 7).

**Live URL (GKE):** `https://<GKE_EXTERNAL_IP>.nip.io` (see GKE deployment section below)

## Architecture

```
                        User / Browser
                              │ HTTPS
                              ▼
                    ┌─────────────────────┐
                    │  GCP Load Balancer  │
                    └─────────┬───────────┘
                              │
                    ┌─────────▼───────────┐
                    │  NGINX Ingress      │  (Kubernetes Ingress Controller)
                    └──────┬──────┬───────┘
                  /api/*   │      │  /*
          ┌───────────────┘      └───────────────────┐
          ▼                                           ▼
 ┌────────────────┐                       ┌────────────────────┐
 │  API Gateway   │                       │  React Frontend    │
 │  (YARP+JWT)    │                       │  (nginx:alpine)    │
 └──────┬─────────┘                       └────────────────────┘
        │
        ├── /api/auth, /api/users  ──►  User Service    → PostgreSQL (users_db)
        ├── /api/events            ──►  Event Service   → PostgreSQL (events_db)
        ├── /api/bookings          ──►  Booking Service → PostgreSQL (bookings_db)
        │                               (3 replicas, HPA 2–5)  → Redis (seat locks)
        └── /api/payments          ──►  Payment Service → PostgreSQL (payments_db)

RabbitMQ (async):
  Booking Service  ──BookingCreated──►  Notification Service
  Payment Service  ──PaymentCompleted─► Notification Service

CI/CD:
  GitHub Push → GitHub Actions → Artifact Registry → kubectl apply → GKE Rolling Update
```

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 9, ASP.NET Core, EF Core 9, JWT |
| Frontend | React 18, TypeScript, Vite, Tailwind CSS v4 |
| Data | PostgreSQL 16 (database-per-service) |
| Caching/Locking | Redis 7 |
| Messaging | RabbitMQ 3 |
| Gateway | YARP 2.3 + AspNetCoreRateLimit |
| Orchestration | Google Kubernetes Engine (Autopilot) |
| Container Registry | Google Artifact Registry |
| Ingress / TLS | NGINX Ingress Controller + cert-manager + Let's Encrypt |
| CI/CD | GitHub Actions + Workload Identity Federation (keyless) |
| Local Dev | Docker Compose |

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for local dev)
- Git

---

## Option A — Local Development (Docker Compose)

```bash
# 1. Clone and enter the project
cd TicketBooking

# 2. Copy and configure environment variables
cp .env.example .env
# Edit .env — set POSTGRES_PASSWORD and JWT_SECRET (min 32 chars)

# 3. Build and start all services
docker compose up --build

# 4. Open the app
open http://localhost:3000
# API Gateway:   http://localhost:5000
# RabbitMQ UI:   http://localhost:15672  (guest / guest)
```

### Seed sample events (local)
```bash
docker compose exec postgres-events psql -U postgres -d events_db \
  -f /dev/stdin < infrastructure/db/seed.sql
```

### Scale booking-service locally (HPA demo)
```bash
docker compose up --scale booking-service=3 -d
```

---

## Option B — GKE Cloud Deployment (Production)

### Prerequisites
- [gcloud CLI](https://cloud.google.com/sdk/docs/install) installed and authenticated
- [Helm 3](https://helm.sh/docs/intro/install/) installed
- A GCP account with $300 free credits
- A GitHub repository with this code

### Step 1: One-time GCP setup

Edit `scripts/gcp-setup.sh` — set `PROJECT_ID` and `GITHUB_REPO` at the top, then run:

```bash
chmod +x scripts/gcp-setup.sh
./scripts/gcp-setup.sh
```

This script automatically:
- Enables required GCP APIs
- Creates Artifact Registry repository
- Creates GKE Autopilot cluster (`us-central1`)
- Creates a Service Account with required roles
- Configures Workload Identity Federation (keyless auth for GitHub Actions)
- Installs NGINX Ingress Controller and cert-manager via Helm
- Gets the external IP and patches `k8s/ingress/ingress.yaml`

### Step 2: Update cert-manager email

Edit `k8s/cert-manager/cluster-issuer.yaml` and replace `your-email@example.com` with your actual email, then:
```bash
kubectl apply -f k8s/cert-manager/cluster-issuer.yaml
```

### Step 3: Add GitHub repository secrets

Go to **GitHub → Settings → Secrets and variables → Actions** and add:

| Secret | Value |
|---|---|
| `GCP_PROJECT_ID` | Your GCP project ID |
| `GKE_CLUSTER_NAME` | `ticket-booking-cluster` |
| `GKE_ZONE` | `us-central1` |
| `WIF_PROVIDER` | Output from setup script |
| `WIF_SERVICE_ACCOUNT` | Output from setup script |
| `POSTGRES_PASSWORD` | Strong password (min 16 chars) |
| `JWT_SECRET` | Random 32+ char string |
| `RABBITMQ_USER` | `ticketbooking` |
| `RABBITMQ_PASS` | Strong password |

### Step 4: Push to GitHub → auto-deploy

```bash
git add .
git commit -m "feat: GKE cloud deployment"
git push origin main
```

GitHub Actions automatically:
1. Builds and tests .NET backend and React frontend
2. Builds all 7 Docker images and pushes to Artifact Registry
3. Substitutes image tags in k8s manifests
4. Creates/updates k8s secrets
5. Applies all manifests to GKE
6. Waits for all rolling deployments to complete

### Step 5: Verify deployment

```bash
kubectl get pods -n ticket-booking       # all Running
kubectl get ingress -n ticket-booking    # shows external IP
kubectl get hpa -n ticket-booking        # HPA for booking-service
```

### Access the app
```
https://<GKE_EXTERNAL_IP>.nip.io          ← Frontend (HTTPS)
https://<GKE_EXTERNAL_IP>.nip.io/api/...  ← API Gateway
```

### RabbitMQ Management UI (port-forward)
```bash
kubectl port-forward svc/rabbitmq 15672:15672 -n ticket-booking
open http://localhost:15672   # ticketbooking / <your RABBITMQ_PASS>
```

### Seed sample events (GKE)
```bash
kubectl exec -it statefulset/postgres-events -n ticket-booking -- \
  psql -U postgres -d events_db -c "$(cat infrastructure/db/seed.sql)"
```

### Demo horizontal autoscaling
```bash
# Manual scale
kubectl scale deployment booking-service --replicas=5 -n ticket-booking

# Watch HPA in action (CPU-triggered)
kubectl get hpa booking-service-hpa -n ticket-booking --watch
```

## API Endpoints

### Auth
| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | — | Register new user |
| POST | `/api/auth/login` | — | Login, returns JWT |
| GET | `/api/users/me` | Bearer | Current user profile |

### Events
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/events` | — | List events (filter: category, city, date) |
| GET | `/api/events/{id}` | — | Event details |
| GET | `/api/events/{id}/seats` | — | Available seats |
| POST | `/api/events` | Admin | Create event |
| PUT | `/api/events/{id}` | Admin | Update event |
| DELETE | `/api/events/{id}` | Admin | Soft-delete event |

### Bookings
| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/bookings` | Bearer | Create booking (Redis seat lock) |
| GET | `/api/bookings/my` | Bearer | User's bookings |
| GET | `/api/bookings/{id}` | Bearer | Booking details |
| POST | `/api/bookings/{id}/cancel` | Bearer | Cancel booking |

### Payments
| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/payments` | Bearer | Process payment (mock) |
| GET | `/api/payments/booking/{id}` | Bearer | Payment for a booking |

## Cloud Computing Concepts Demonstrated

| Concept | Implementation |
|---|---|
| **Horizontal Scaling** | booking-service HPA (2–5 pods, CPU 60% threshold) |
| **High Availability** | 2+ replicas for gateway + frontend; Kubernetes self-healing |
| **Distributed Locking** | Redis SETNX prevents double-booking across pod replicas |
| **Async Messaging** | RabbitMQ decouples booking/payment from notifications |
| **Sync Communication** | REST/HTTP between services via Kubernetes internal DNS |
| **Security** | JWT Bearer auth, rate limiting (100 req/min), RBAC, HTTPS |
| **Infrastructure as Code** | All k8s resources in YAML, managed via Git |
| **CI/CD** | GitHub Actions: build → push → deploy on every `git push` |
| **Zero-downtime Deploy** | Kubernetes rolling updates (one pod at a time) |
| **Data Isolation** | Database-per-service (4 independent PostgreSQL StatefulSets) |
| **Certificate Management** | cert-manager + Let's Encrypt auto-renews TLS cert |
| **Observability** | `/health` endpoint on every service; kubectl logs + events |

## Project Structure

```
TicketBooking/
├── gateway/TicketBooking.Gateway/        YARP API Gateway
├── services/
│   ├── TicketBooking.UserService/        Auth + Users
│   ├── TicketBooking.EventService/       Events + Seats
│   ├── TicketBooking.BookingService/     Reservations + Redis locking
│   ├── TicketBooking.PaymentService/     Mock payments
│   └── TicketBooking.NotificationService/ RabbitMQ consumer
├── shared/TicketBooking.Shared/          DTOs + Message contracts
├── frontend/ticket-booking-ui/           React 18 + TypeScript
├── infrastructure/
│   ├── nginx/nginx.conf                  NGINX LB (local dev)
│   └── db/seed.sql                       Sample event data
├── k8s/                                  ← All Kubernetes manifests
│   ├── namespace.yaml
│   ├── configmap/                        Non-secret env vars
│   ├── infrastructure/                   Postgres, Redis, RabbitMQ
│   ├── services/                         5 microservice deployments
│   ├── gateway/                          API Gateway deployment
│   ├── frontend/                         Frontend deployment
│   ├── ingress/                          NGINX Ingress + TLS
│   ├── hpa/                              HorizontalPodAutoscaler
│   └── cert-manager/                     Let's Encrypt ClusterIssuer
├── scripts/
│   └── gcp-setup.sh                      One-time GCP/GKE setup
├── .github/workflows/
│   ├── ci.yml                            PR validation (build only)
│   └── gke-deploy.yml                    Full deploy pipeline
├── docker-compose.yml                    Local development
└── .env.example
```
