# Description
Service Network provides the 5D-I-HCDT platform with access to services that deliver optimization, predictive maintenance, and insights to users. These services are deployed within a service network that ensures secure, encrypted communication, call authorization, traceability, and service access to data in the AAS and data storage systems.

# Scheme
<img width="1822" height="778" alt="image" src="https://github.com/user-attachments/assets/e92080fe-5e99-47f6-b813-2c81a5b7b7bb" />

# Local setup
1. Register and login to DuckDNS - https://www.duckdns.org/
2. Create a subdomain, e.g. ***expi40test3*** (must be unique)
3. Create environment variables:
   - DUCKDNS_TOKEN : ***DuckDNS token***
   - DUCKDNS_DOMAINS: ****.expi40test3***.duckdns.org
     Note: "\*" ensures wildcard certificate will be issued.
4. Clone repository https://dev.azure.com/EXPI40/LLM-Chatbot/_git/LLM-ChatBot
5. Update .env file
   - DUCKDNS_SUBDOMAIN=***expi40test3***
   - HOST_IP - *no changes needed*
6. Run Powershell as Administrator
7. Install Posh-ACME module

   `Install-Module -Name Posh-ACME -Scope CurrentUser -Force`
8. Enable running scripts in this system, if disabled

   `Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope CurrentUser`
9. Run `.\certs-renewal.ps1`
10. When prompted insert:
   - DuckToken: ***DuckDNS token*** *(ENTER)*
   - DuckDomain[0]: ***expi40test3***.duckdns.org *(ENTER)*
     Note: Do not use wildcard ("\*") domain!
   - DuckDomain[1]: *(ENTER)*
11. After 120 seconds (needed to propagate DNS changes), when prompted insert:
   - DuckToken: ***DuckDNS token*** *(ENTER)*
   - DuckDomain[0]: ***expi40test3***.duckdns.org *(ENTER)*
     Note: Do not use wildcard ("\*") domain!
   - DuckDomain[1]: *(ENTER)*
12. Run `.\generate-configs.ps1`
13. Update hosts entries (C:\Windows\System32\drivers\etc)

```
# LLM ChatBot
127.0.0.1 auth.*expi40test3*.duckdns.org
127.0.0.1 ui.*expi40test3*.duckdns.org
127.0.0.1 aas.*expi40test3*.duckdns.org
127.0.0.1 aas-registry.*expi40test3*.duckdns.org
127.0.0.1 sm-registry.*expi40test3*.duckdns.org
127.0.0.1 discovery.*expi40test3*.duckdns.org
127.0.0.1 dashboard.*expi40test3*.duckdns.org
127.0.0.1 chatbot.*expi40test3*.duckdns.org
```

14. `docker compose up -d`
