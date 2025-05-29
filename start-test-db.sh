#!/bin/zsh

# Script khởi chạy SQL Server trong Docker cho testing
echo "Khởi chạy SQL Server Container cho testing..."

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

# Khởi chạy SQL Server container cho testing
echo "Khởi động SQL Server container cho testing..."
docker compose -f compose.test.yaml up -d

# Đợi container khởi động
echo "Đợi SQL Server khởi động..."
sleep 20

# Kiểm tra container đã chạy chưa
if ! docker ps | grep -q "testawing-test-sqlserver"
then
    echo "Không thể khởi động SQL Server container. Vui lòng kiểm tra lỗi."
    exit 1
fi

echo "SQL Server đã sẵn sàng cho testing tại localhost:1434"
echo "User: sa"
echo "Password: TestPassword@2024"
echo "Database: TreasureHuntTestDb (sẽ được tự động tạo khi chạy test)"
echo ""
echo "Chạy tests với lệnh:"
echo "dotnet test TestAwing.Tests/TestAwing.Tests.csproj --filter FullyQualifiedName~TreasureHuntSolverServiceSqlServerTests"
