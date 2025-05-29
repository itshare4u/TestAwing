#!/bin/bash
set -e

# Function to wait for SQL Server to be available
wait_for_sqlserver() {
    echo "Waiting for SQL Server to be available..."
    
    # Use sqlcmd to check if SQL Server is available (using mssql-tools18)
    until /opt/mssql-tools18/bin/sqlcmd -S $DB_HOST -U $DB_USER -P $DB_PASSWORD -C -Q "SELECT 1" &> /dev/null
    do
        echo "SQL Server is unavailable - sleeping 5 seconds"
        sleep 5
    done
    
    echo "SQL Server is up - continuing"
}

# Check if we're using SQL Server
if [[ "$DatabaseProvider" == "SqlServer" ]]; then
    # Extract DB host, user and password from connection string
    DB_HOST=$(echo $ConnectionStrings__DefaultConnection | grep -oP 'Server=\K[^,;]+')
    DB_USER=$(echo $ConnectionStrings__DefaultConnection | grep -oP 'User=\K[^,;]+')
    DB_PASSWORD=$(echo $ConnectionStrings__DefaultConnection | grep -oP 'Password=\K[^,;]+')
    
    # If we have all the required values, wait for SQL Server
    if [[ -n "$DB_HOST" && -n "$DB_USER" && -n "$DB_PASSWORD" ]]; then
        wait_for_sqlserver
    else
        echo "SQL Server connection settings not found in connection string"
    fi
fi

# Start the application
exec dotnet TestAwing.dll
