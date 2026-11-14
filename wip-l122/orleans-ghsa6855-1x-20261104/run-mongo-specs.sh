#!/usr/bin/env bash
# CI mirror for 1.x ordinary MongoDB specs: dotnet-build.yml runs them with a mongo:7 service on 27017.
set -u
cd /Volumes/sourcecode/repos/cratis/.worktrees/orleans-ghsa6855-1x
cleanup() { docker rm -f ghsa6855x-mongo-specs >/dev/null 2>&1 || true; }
trap cleanup EXIT
cleanup
docker run -d --rm --name ghsa6855x-mongo-specs -p 27017:27017 mongo:7 >/dev/null || exit 70
for i in $(seq 1 30); do docker exec ghsa6855x-mongo-specs mongosh --quiet --eval 'db.runCommand({ping:1}).ok' >/dev/null 2>&1 && break; sleep 2; done
bash .github/scripts/run-specifications.sh Release --scope ordinary --project Source/Orleans.Storage.MongoDB.Specs/Orleans.Storage.MongoDB.Specs.csproj --results .ai-work/mongo-results
