param(
	[string]$envFile
)

# Load env file specified in args into environment variables
Get-Content -Path ..\envs\$envFile | ForEach-Object {
    # Trim whitespace
    $line = $_.Trim()

    # Skip empty lines & full-line comments
    if ($line -eq "" -or $line.StartsWith("#")) { return }

    # Remove inline comments (anything after #)
    $cleanLine = $line -replace "\s+#.*$",""

    # Match KEY=VALUE pairs
    if ($cleanLine -match "^([A-Za-z_][A-Za-z0-9_]*)=(.*)$") {
        $name = $matches[1]
        $value = $matches[2].Trim()

        Set-Item -Path "env:$name" -Value $value
    }
}

# List of template files and their output paths
$templates = @(
	# basyx
    @{ Template = "../basyx-setup/basyx/aas-dashboard.yml.template"; Output = "../basyx-setup/basyx/aas-dashboard.yml" },
    @{ Template = "../basyx-setup/basyx/aas-discovery.properties.template"; Output = "../basyx-setup/basyx/aas-discovery.properties" },
    @{ Template = "../basyx-setup/basyx/aas-env.properties.template"; Output = "../basyx-setup/basyx/aas-env.properties" },
    @{ Template = "../basyx-setup/basyx/aas-registry.yml.template"; Output = "../basyx-setup/basyx/aas-registry.yml" },
    @{ Template = "../basyx-setup/basyx/sm-registry.yml.template"; Output = "../basyx-setup/basyx/sm-registry.yml" },
	# coredns
    @{ Template = "../coredns/Corefile.template"; Output = "../coredns/Corefile" },
	# influx
    @{ Template = "../influxdb/config/influx-configs.template"; Output = "../influxdb/config/influx-configs" },
	# keycloak
    @{ Template = "../keycloak/realm-export.json.template"; Output = "../keycloak/realm-export.json" },
	# mosquitto
    @{ Template = "../mosquitto/config/mosquitto.conf.template"; Output = "../mosquitto/config/mosquitto.conf" },
	# telegraf
    @{ Template = "../telegraf/telegraf.conf.template"; Output = "../telegraf/telegraf.conf" },
	# traefik
    @{ Template = "../traefik/dynamic.yml.template"; Output = "../traefik/dynamic.yml" }
)

# Generate files from templates
foreach ($t in $templates) {
    (Get-Content $t.Template) | ForEach-Object {
        $_  -replace '\$\{DUCKDNS_SUBDOMAIN\}', $env:DUCKDNS_SUBDOMAIN `
		    -replace '\$\{HOST_IP\}', $env:HOST_IP `
			-replace '\$\{MONGO_USER\}', $env:MONGO_INITDB_ROOT_USERNAME `
			-replace '\$\{MONGO_PASSWORD\}', $env:MONGO_INITDB_ROOT_PASSWORD `
			-replace '\$\{MONGO_PORT\}', $env:MONGO_PORT `
			-replace '\$\{MOSQUITTO_PORT\}', $env:MOSQUITTO_PORT `
            -replace '\$\{MOSQUITTO_ADMIN_USER\}', $env:MOSQUITTO_ADMIN_USER `
            -replace '\$\{MOSQUITTO_ADMIN_PASSWORD\}', $env:MOSQUITTO_ADMIN_PASSWORD `
            -replace '\$\{MOSQUITTO_PLC_USER\}', $env:MOSQUITTO_PLC_USER `
            -replace '\$\{MOSQUITTO_PLC_PASSWORD\}', $env:MOSQUITTO_PLC_PASSWORD `
            -replace '\$\{MOSQUITTO_CLIENT_USER\}', $env:MOSQUITTO_CLIENT_USER `
            -replace '\$\{MOSQUITTO_CLIENT_PASSWORD\}', $env:MOSQUITTO_CLIENT_PASSWORD `
			-replace '\$\{INFLUX_TOKEN\}', $env:DOCKER_INFLUXDB_INIT_ADMIN_TOKEN `
			-replace '\$\{INFLUX_ORG\}', $env:DOCKER_INFLUXDB_INIT_ORG `
			-replace '\$\{INFLUX_BUCKET\}', $env:DOCKER_INFLUXDB_INIT_BUCKET `
			-replace '\$\{INFLUX_PORT\}', $env:INFLUX_PORT `
			-replace '\$\{KEYCLOAK_REALM\}', $env:KEYCLOAK_REALM `
			-replace '\$\{KEYCLOAK_CLIENT_ID\}', $env:KEYCLOAK_CLIENT_ID `
			-replace '\$\{KEYCLOAK_TEST_USER\}', $env:KEYCLOAK_TEST_USER `
			-replace '\$\{KEYCLOAK_TEST_USER_PASSWORD\}', $env:KEYCLOAK_TEST_USER_PASSWORD `
    } | Set-Content $t.Output
}