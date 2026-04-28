#!/usr/bin/env bash
# =============================================================================
# GCP One-Time Setup Script — Event Ticket Booking Platform
# Run this ONCE before pushing to GitHub.
# Prerequisites: gcloud CLI installed and authenticated (gcloud auth login)
# =============================================================================
set -euo pipefail

# ─── CONFIGURE THESE BEFORE RUNNING ─────────────────────────────────────────
PROJECT_ID=""          # e.g. my-project-123456
REGION="us-central1"
ZONE="us-central1-a"
CLUSTER_NAME="ticket-booking-cluster"
AR_REPO="ticket-booking"
GITHUB_REPO=""         # e.g. myusername/ticket-booking  (owner/repo)
SA_NAME="github-actions-sa"
# ─────────────────────────────────────────────────────────────────────────────

if [[ -z "$PROJECT_ID" || -z "$GITHUB_REPO" ]]; then
  echo "ERROR: Set PROJECT_ID and GITHUB_REPO at the top of this script before running."
  exit 1
fi

echo "==> Setting active GCP project to $PROJECT_ID"
gcloud config set project "$PROJECT_ID"

# ─── 1. Enable required APIs ─────────────────────────────────────────────────
echo ""
echo "==> [1/7] Enabling required GCP APIs..."
gcloud services enable \
  container.googleapis.com \
  artifactregistry.googleapis.com \
  iam.googleapis.com \
  iamcredentials.googleapis.com \
  cloudresourcemanager.googleapis.com \
  --quiet

# ─── 2. Create Artifact Registry repository ──────────────────────────────────
echo ""
echo "==> [2/7] Creating Artifact Registry repository: $AR_REPO"
gcloud artifacts repositories create "$AR_REPO" \
  --repository-format=docker \
  --location="$REGION" \
  --description="Docker images for Ticket Booking Platform" \
  --quiet 2>/dev/null || echo "  Repository already exists – skipping."

# ─── 3. Create GKE Autopilot cluster ─────────────────────────────────────────
echo ""
echo "==> [3/7] Creating GKE Autopilot cluster: $CLUSTER_NAME (this takes ~5 min)..."
gcloud container clusters create-auto "$CLUSTER_NAME" \
  --region="$REGION" \
  --quiet 2>/dev/null || echo "  Cluster already exists – skipping."

# ─── 4. Create GitHub Actions Service Account ────────────────────────────────
echo ""
echo "==> [4/7] Creating service account: $SA_NAME"
gcloud iam service-accounts create "$SA_NAME" \
  --display-name="GitHub Actions – Ticket Booking" \
  --quiet 2>/dev/null || echo "  Service account already exists – skipping."

SA_EMAIL="${SA_NAME}@${PROJECT_ID}.iam.gserviceaccount.com"

# Grant required roles
echo "  Granting roles to $SA_EMAIL..."
gcloud projects add-iam-policy-binding "$PROJECT_ID" \
  --member="serviceAccount:$SA_EMAIL" \
  --role="roles/artifactregistry.writer" \
  --quiet

gcloud projects add-iam-policy-binding "$PROJECT_ID" \
  --member="serviceAccount:$SA_EMAIL" \
  --role="roles/container.developer" \
  --quiet

# ─── 5. Set up Workload Identity Federation for GitHub Actions ────────────────
echo ""
echo "==> [5/7] Configuring Workload Identity Federation..."
POOL_NAME="github-actions-pool"
PROVIDER_NAME="github-actions-provider"

# Create the Workload Identity Pool
gcloud iam workload-identity-pools create "$POOL_NAME" \
  --location="global" \
  --display-name="GitHub Actions Pool" \
  --quiet 2>/dev/null || echo "  Pool already exists – skipping."

# Create the OIDC provider
gcloud iam workload-identity-pools providers create-oidc "$PROVIDER_NAME" \
  --location="global" \
  --workload-identity-pool="$POOL_NAME" \
  --display-name="GitHub Actions Provider" \
  --issuer-uri="https://token.actions.githubusercontent.com" \
  --attribute-mapping="google.subject=assertion.sub,attribute.repository=assertion.repository,attribute.actor=assertion.actor,attribute.ref=assertion.ref" \
  --attribute-condition="assertion.repository=='${GITHUB_REPO}'" \
  --quiet 2>/dev/null || echo "  Provider already exists – skipping."

# Get full pool/provider resource names
POOL_RESOURCE=$(gcloud iam workload-identity-pools describe "$POOL_NAME" \
  --location="global" --format="value(name)")

PROVIDER_RESOURCE=$(gcloud iam workload-identity-pools providers describe "$PROVIDER_NAME" \
  --location="global" \
  --workload-identity-pool="$POOL_NAME" \
  --format="value(name)")

# Allow GitHub repo to impersonate the service account
gcloud iam service-accounts add-iam-policy-binding "$SA_EMAIL" \
  --role="roles/iam.workloadIdentityUser" \
  --member="principalSet://iam.googleapis.com/${POOL_RESOURCE}/attribute.repository/${GITHUB_REPO}" \
  --quiet

# ─── 6. Install Helm tools on GKE ────────────────────────────────────────────
echo ""
echo "==> [6/7] Getting cluster credentials and installing NGINX Ingress + cert-manager..."
gcloud container clusters get-credentials "$CLUSTER_NAME" --region="$REGION" --quiet

# NGINX Ingress Controller
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx 2>/dev/null || true
helm repo update
helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace ingress-nginx --create-namespace \
  --set controller.replicaCount=2 \
  --wait --timeout 5m

# cert-manager
helm repo add jetstack https://charts.jetstack.io 2>/dev/null || true
helm repo update
helm upgrade --install cert-manager jetstack/cert-manager \
  --namespace cert-manager --create-namespace \
  --set crds.enabled=true \
  --wait --timeout 5m

# Apply cert-manager ClusterIssuer
echo "  Applying Let's Encrypt ClusterIssuer..."
echo "  NOTE: Edit k8s/cert-manager/cluster-issuer.yaml and set your email first!"
kubectl apply -f k8s/cert-manager/cluster-issuer.yaml

# ─── 7. Get external IP and update Ingress ───────────────────────────────────
echo ""
echo "==> [7/7] Waiting for NGINX Ingress external IP (may take 2-3 min)..."
for i in $(seq 1 30); do
  EXTERNAL_IP=$(kubectl get svc ingress-nginx-controller \
    -n ingress-nginx \
    -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || echo "")
  if [[ -n "$EXTERNAL_IP" ]]; then
    break
  fi
  echo "  Waiting... ($i/30)"
  sleep 10
done

if [[ -z "$EXTERNAL_IP" ]]; then
  echo "  WARNING: Could not get external IP automatically."
  echo "  Run: kubectl get svc -n ingress-nginx ingress-nginx-controller"
  echo "  Then manually update k8s/ingress/ingress.yaml replacing GKE_EXTERNAL_IP"
else
  echo "  External IP: $EXTERNAL_IP"
  echo "  Updating k8s/ingress/ingress.yaml with IP: $EXTERNAL_IP"
  sed -i "s/GKE_EXTERNAL_IP/${EXTERNAL_IP}/g" k8s/ingress/ingress.yaml
  kubectl apply -f k8s/cert-manager/cluster-issuer.yaml
fi

# ─── Print GitHub Actions secrets to configure ───────────────────────────────
echo ""
echo "============================================================"
echo "  SETUP COMPLETE"
echo "============================================================"
echo ""
echo "Add these secrets to your GitHub repository"
echo "(Settings → Secrets and variables → Actions → New repository secret):"
echo ""
echo "  GCP_PROJECT_ID       = $PROJECT_ID"
echo "  GKE_CLUSTER_NAME     = $CLUSTER_NAME"
echo "  GKE_ZONE             = $REGION"
echo "  WIF_PROVIDER         = $PROVIDER_RESOURCE"
echo "  WIF_SERVICE_ACCOUNT  = $SA_EMAIL"
echo "  POSTGRES_PASSWORD    = <choose a strong password>"
echo "  JWT_SECRET           = <choose a 32+ char secret>"
echo "  RABBITMQ_USER        = ticketbooking"
echo "  RABBITMQ_PASS        = <choose a password>"
echo ""
if [[ -n "$EXTERNAL_IP" ]]; then
  echo "Application URL (after deploy): https://${EXTERNAL_IP}.nip.io"
fi
echo ""
echo "Next steps:"
echo "  1. Edit k8s/cert-manager/cluster-issuer.yaml — set your email"
echo "  2. Add all GitHub secrets listed above"
echo "  3. git push origin main — CI/CD deploys automatically"
echo "============================================================"
