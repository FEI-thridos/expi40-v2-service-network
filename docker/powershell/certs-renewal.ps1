# Posh-ACME module has to be installed:
# Install-Module -Name Posh-ACME -Scope CurrentUser -Force
# Enable running scripts in this system, if disabled:
# Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope CurrentUser

# Load the Posh-ACME module
Import-Module Posh-ACME

# Set Let's Encrypt production server
Set-PAServer LE_PROD

# Check if DuckDNS token is set
if (-not $env:DUCKDNS_TOKEN) {
    Write-Error "DUCKDNS_TOKEN environment variable not set. Please define it in system or user environment variables."
    exit 1
}

# Check if duckdns domains are set
if (-not $env:DUCKDNS_DOMAINS) {
    Write-Error "DUCKDNS_DOMAINS environment variable not set. Please define it as a comma-separated list (e.g., 'subdomain.duckdns.org,*.subdomain.duckdns.org')."
    exit 1
}

# Split the domains into an array (handles comma-separated list)
$domains = $env:DUCKDNS_DOMAINS -split ',' | ForEach-Object { $_.Trim() }

# Configure DuckDNS plugin parameters
$pArgs = @{
    DDToken = $env:DUCKDNS_TOKEN
}

# Renew/Create certificates for each domain
foreach ($domain in $domains) {
    echo "Processing renewal for $domain"
    # try {
      # Submit-Renewal -Domain $domain -Plugin DuckDNS -PluginArgs $pArgs -ErrorAction Stop
    # } catch {
      # New-PACertificate -Domain $domain -AcceptTOS -Plugin DuckDNS -PluginArgs $pArgs -Force
    # }
	New-PACertificate -Domain $domain -AcceptTOS -Plugin DuckDNS -PluginArgs $pArgs -Force -Verbose -UseSerialValidation
}

# Check whether ./certs directory exists
$destPath = Join-Path $PSScriptRoot "certs"
if (-not (Test-Path $destPath)) {
    New-Item -ItemType Directory -Path $destPath -Force | Out-Null
}

# Copy certificates to ./certs for all domains
foreach ($domain in $domains) {
    $sourcePath = Join-Path $env:LOCALAPPDATA "Posh-ACME\LE_PROD\*\${domain}\*"
    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $destPath -Recurse -Force
        echo "Certificates for $domain copied to $destPath"
    } else {
        Write-Error "Certificate files not found for $domain at $sourcePath"
    }
}

# Touch ../traefik/dynamic.yml and ensure Traefik reloads to force certificate reload
$dynamicPath = Join-Path $PSScriptRoot "..\traefik\dynamic.yml"
if (Test-Path $dynamicPath) {
	docker-compose -f "..\docker-compose.yml" restart traefik
    echo "Restarted Traefik to load new certificates from $destPath"
} else{
	Write-Error "Could not restart Traefik"
	exit 1
}