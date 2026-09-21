#!/bin/sh

# Define password file location
PASSWORD_FILE=/mosquitto/config/passwordfile
# Define role access file location
ACL_FILE=/mosquitto/config/aclfile

# Define users from environment variables
admin_user=$MOSQUITTO_ADMIN_USER # used for all services (basyx-databridge, basyx-aas-environment, ...)
admin_pass=$MOSQUITTO_ADMIN_PASSWORD
plc_user=$MOSQUITTO_PLC_USER # used for PLC connections
plc_pass=$MOSQUITTO_PLC_PASSWORD
client_user=$MOSQUITTO_CLIENT_USER # used for HoloLens clients
client_pass=$MOSQUITTO_CLIENT_PASSWORD

# Validate required variables
required_vars="$admin_user $admin_pass $plc_user $plc_pass $client_user $client_pass"
for var in $required_vars; do
  if [ -z "$var" ]; then
    echo "ERROR: One or more MQTT user/password environment variables are missing!"
    exit 1
  fi
done

# Create password file if missing
if [ ! -f "$PASSWORD_FILE" ]; then
  echo "Initializing Mosquitto users..."
  mosquitto_passwd -c -b $PASSWORD_FILE "$admin_user" "$admin_pass"
  mosquitto_passwd -b $PASSWORD_FILE "$plc_user" "$plc_pass"
  mosquitto_passwd -b $PASSWORD_FILE "$client_user" "$client_pass"
fi

# Create ACL file if missing
if [ ! -f "$ACL_FILE" ]; then
  echo "Initializing Mosquitto ACL..."
  
  # Client
  echo "user $client_user" > "$ACL_FILE"
  echo "topic read plc" >> "$ACL_FILE"
  echo "topic write client/#" >> "$ACL_FILE"

  # PLC
  echo "" >> "$ACL_FILE"
  echo "user $plc_user" >> "$ACL_FILE"
  echo "topic write plc" >> "$ACL_FILE"

  # Admin (full access)
  echo "" >> "$ACL_FILE"
  echo "user $admin_user" >> "$ACL_FILE"
  echo "topic readwrite #" >> "$ACL_FILE"
fi

echo "Starting Mosquitto..."
exec mosquitto -c /mosquitto/config/mosquitto.conf