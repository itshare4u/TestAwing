#!/bin/zsh

# Script khởi chạy production environment với SQL Server
echo "Khởi chạy Production Environment với SQL Server..."

# Kiểm tra xem Docker đã được cài đặt chưa
if ! command -v docker &> /dev/null
then
    echo "Docker chưa được cài đặt. Vui lòng cài đặt Docker trước."
    exit 1
fi

# Kiểm tra xem docker compose có sẵn không
if ! command -v docker compose &> /dev/null
then
    echo "Docker Compose chưa được cài đặt. Vui lòng cài đặt Docker Compose trước."
    exit 1
fi

# Build và khởi chạy các containers
echo "Build và khởi động containers..."
docker compose build
docker compose up -d

# Đợi một chút để các container khởi động
sleep 10

# Kiểm tra trạng thái
echo "\nTrạng thái các containers:"
docker compose ps

# Kiểm tra log của backend để xem kết nối database
echo "\nLog của backend:"
docker compose logs testawing-backend --tail 20

echo "\nThông tin kết nối SQL Server:"
echo "Server: localhost,1433"
echo "Database: TreasureHuntDb"
echo "User: sa"
echo "Password: TreasureHunt@2024"

echo "\nÚng dụng đã được khởi chạy:"
echo "- Frontend: http://localhost:3001"
echo "- Backend API: http://localhost:5001"
