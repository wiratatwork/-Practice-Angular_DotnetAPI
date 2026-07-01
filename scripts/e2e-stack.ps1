param(
    [ValidateSet('up', 'down')]
    [string]$Action = 'up'
)

$ErrorActionPreference = 'Stop'

$RootDir = Split-Path -Parent $PSScriptRoot
$ProjectName = if ($env:COMPOSE_PROJECT_NAME) { $env:COMPOSE_PROJECT_NAME } else { 'basic-app-e2e' }
$ComposeArgs = @('-p', $ProjectName, '-f', 'docker-compose.yml', '-f', 'docker-compose.e2e.yml')

Push-Location $RootDir
try {
    switch ($Action) {
        'up' {
            docker compose @ComposeArgs up -d --build --wait postgres api frontend
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
            bash ./scripts/smoke-test.sh
        }
        'down' {
            docker compose @ComposeArgs down -v --remove-orphans
        }
    }
} finally {
    Pop-Location
}
