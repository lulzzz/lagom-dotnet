#!/bin/bash

# Wait for database
for i in {30..0}; do
  if sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q 'SELECT 1;' &> /dev/null; then
    echo "$0: SQL Server started"
    break
  fi
  echo "$0: SQL Server startup in progress..."
  sleep 1
done

# Check to see if migration is required
FILE=/app/up.sql
if [ ! -f $FILE ]; then
	echo "migration not required. exiting..."
	exit
fi

# Create database
echo "$0: Creating database"
sqlcmd -S localhost -U sa -P $SA_PASSWORD -d master -Q 'create database wyvern;' &> /dev/null
echo "$0: Database created"

# Perform migration
echo "$0: Performing initial migration"
sqlcmd -S localhost -U sa -P $SA_PASSWORD -d wyvern -i /app/up.sql
rm /app/up.sql
echo "$0: Performing initial migration - complete"
