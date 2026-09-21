#!/bin/bash

set -e
set -u

# Function to create a database and user
create_user_and_database() {
  local db_name=$(echo $1 | tr ':' ' ' | awk  '{print $1}')
  local db_user=$(echo $1 | tr ':' ' ' | awk  '{print $2}')
  local db_pass=$(echo $1 | tr ':' ' ' | awk  '{print $3}')
  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
    -- Create the user
    CREATE USER $db_user WITH PASSWORD '$db_pass';
    -- Create the database
    CREATE DATABASE $db_name;
    -- Grant full privileges on the database
    GRANT ALL PRIVILEGES ON DATABASE $db_name TO $db_user;
    -- Connect to the database
    \c $db_name
    -- Grant full control over the public schema
    GRANT ALL PRIVILEGES ON SCHEMA public TO $db_user;
    -- Ensure the user has privileges on all existing and future objects
    ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO $db_user;
    ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO $db_user;
    ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON FUNCTIONS TO $db_user;
    -- Optionally, make the user the owner of the database (full admin control)
    ALTER DATABASE $db_name OWNER TO $db_user;
EOSQL
}

# Create databases and users from POSTGRES_MULTIPLE_DATABASES
if [ -n "$POSTGRES_MULTIPLE_DATABASES" ]; then
	echo "Multiple database creation requested: $POSTGRES_MULTIPLE_DATABASES"
	for db in $(echo $POSTGRES_MULTIPLE_DATABASES | tr ',' ' '); do
		create_user_and_database $db
	done
	echo "Multiple databases created"
fi