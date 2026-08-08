# Setup Guide

## Prerequisites

- Docker Desktop running
- .NET 6+ SDK

## Quick Start

```bash
cd 02-full-stack-integration

# 1. Start services (API, UI, PostgreSQL)
docker-compose up -d

# 2. Verify (wait 30-60s)
curl http://localhost:3000/health  # Should return "healthy"

## Services

- **API** (3000): Node.js + PostgreSQL, logs: `docker logs promotion-api`
- **UI** (8080): http://localhost:8080 - Laptop ($1000, ELECTRONICS)
- **DB** (5432): testshop/testuser/testpass - tables: promotions, products, orders, promotion_audit_log
