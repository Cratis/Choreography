#!/usr/bin/env bash
# CI mirror (dotnet-build.yml) for Integration/Jobs.Integration: mongo:7, postgres:17 (trust), mssql 2022.
set -u
cd /Volumes/sourcecode/repos/cratis/.worktrees/orleans-ghsa6855x-1x
pw="$(openssl rand -hex 16)Aa1!"
cleanup() { docker rm -f ghsa6855x-mongo ghsa6855x-pg ghsa6855x-sql >/dev/null 2>&1 || true; }
trap cleanup EXIT
cleanup
docker run -d --rm --name ghsa6855x-mongo -p 27017:27017 mongo:7 >/dev/null || exit 70
docker run -d --rm --name ghsa6855x-pg -p 5432:5432 -e POSTGRES_HOST_AUTH_METHOD=trust postgres:17 >/dev/null || exit 71
docker run -d --rm --name ghsa6855x-sql -p 1433:1433 -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=$pw" mcr.microsoft.com/mssql/server:2022-latest >/dev/null || exit 72
for i in $(seq 1 20); do docker exec ghsa6855x-pg pg_isready -U postgres >/dev/null 2>&1 && break; sleep 5; done
docker exec ghsa6855x-pg pg_isready -U postgres >/dev/null 2>&1 || { echo "postgres not ready"; exit 73; }
for i in $(seq 1 90); do docker logs ghsa6855x-sql 2>&1 | grep -q "SQL Server is now ready for client connections" && break; sleep 2; done
docker logs ghsa6855x-sql 2>&1 | grep -q "SQL Server is now ready for client connections" || { echo "sqlserver not ready"; exit 74; }
export CRATIS_ORLEANS_SPECS_POSTGRESQL="Host=localhost;Port=5432;Username=postgres;Database=postgres"
export CRATIS_ORLEANS_SPECS_SQLSERVER="Server=localhost,1433;User Id=sa;Password=$pw;TrustServerCertificate=True;Encrypt=False"
echo "services ready; load: $(uptime)"
bash .github/scripts/run-specifications.sh Release --scope integration --results .ai-work/int-results-${INT_RUN:-all}
rc=$?
echo "load after: $(uptime)"
exit $rc
