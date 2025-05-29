#!/bin/zsh

# Script dừng SQL Server container cho testing
echo "Dừng SQL Server Container cho testing..."

# Kiểm tra xem Docker đã được cài đặt chưa
if ! command -v docker &> /dev/null
then
    echo "Docker chưa được cài đặt."
    exit 1
fi

# Kiểm tra xem docker compose có sẵn không
if ! command -v docker compose &> /dev/null
then
    echo "Docker Compose chưa được cài đặt."
    exit 1
fi

# Dừng container
echo "Dừng SQL Server container..."
docker compose -f compose.test.yaml down

echo "SQL Server container đã được dừng."
