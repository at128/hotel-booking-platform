# Monitoring Alerts Runbook

This project uses `Serilog` with `Seq` as the primary log and monitoring surface.

## Access

- Seq URL: `http://<host>:5341`
- Login: local Seq admin account (`admin`) with `SEQ_ADMIN_PASSWORD`
- Correlation key for tracing requests end-to-end: `CorrelationId`

## Required Baseline Signals (Seq)

Create these signals and connect them to notification apps (Email/Slack/Teams/Webhook):

1. `critical_unhandled_exceptions`
- Query: `@Level = 'Error' and @MessageTemplate like 'Unhandled exception:%'`
- Alert: trigger if count `>= 1` in `5m`
- Severity: Critical

2. `payment_webhook_failures`
- Query: `@MessageTemplate like '%Payment {PaymentId} failed for booking {BookingNumber}%'`
- Alert: trigger if count `>= 3` in `10m`
- Severity: High

3. `refund_gateway_exceptions`
- Query: `@MessageTemplate like '%Refund gateway exception for cancellation%'`
- Alert: trigger if count `>= 3` in `10m`
- Severity: High

4. `db_connectivity_startup_failures`
- Query: `@MessageTemplate like 'Cannot connect to database on startup.'`
- Alert: trigger if count `>= 1` in `5m`
- Severity: Critical

5. `rate_limit_spike`
- Query: `RequestPath is not null and StatusCode = 429`
- Alert: trigger if count `>= 100` in `5m`
- Severity: Medium

## Health Monitoring

- Endpoints:
  - `/api/v1/health/live`
  - `/api/v1/health/ready`
- Default policy is admin-only unless `Monitoring:AllowAnonymousHealthEndpoints=true`.
- For deployment probes in this repo, CD sets `Monitoring__AllowAnonymousHealthEndpoints=true` by default.

## Dashboard Panels (minimum)

1. Requests per minute (`RequestPath`, `StatusCode`)
2. 5xx count over time
3. 429 count over time
4. Payment success vs failure events
5. Slow request warnings (`Long running request`)

## Incident Triage Flow

1. Open Seq and filter by time range `Last 15 minutes`.
2. Inspect failing signal and pivot by `CorrelationId`.
3. Confirm blast radius:
- single endpoint or multiple endpoints
- single user/tenant/IP or global
4. If payment-related, check webhook logs first, then booking/payment status.
5. If infra-related, check DB connectivity and health endpoints.
6. Mitigate:
- rollback latest deploy if regression
- scale/limit problematic traffic if abuse
- rotate/reload broken credentials if auth/provider failures
7. Document timeline and root cause in postmortem.
