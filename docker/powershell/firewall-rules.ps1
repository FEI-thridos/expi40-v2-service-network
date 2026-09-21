$fwRules = @(
    @{ Name = "MQTT Port"; Port = 1883 }
    #@{ Name = "Another Port"; Port = 1234 }
)

foreach ($rule in $fwRules) {
    $name = $rule.Name
    $port = $rule.Port

    # Remove old rule if exists
    $existing = Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue
    if ($existing) {
        Remove-NetFirewallRule -DisplayName $name
    }

    # Create new rule
    New-NetFirewallRule `
        -DisplayName $name `
        -Direction Inbound `
        -Action Allow `
        -Protocol TCP `
        -LocalPort $port `
        -Profile Any
}