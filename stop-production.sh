#!/bin/zsh

# Script dừng production environment
echo "Dừng Production Environment..."

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

# Dừng các containers
echo "Dừng các containers..."
docker compose down

echo "Production Environment đã được dừng."
