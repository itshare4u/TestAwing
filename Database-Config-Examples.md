# Database Configuration Examples

## 1. SQLite (File-based, no server required)

```json
{
  "DatabaseProvider": "SQLite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=treasurehunt.db"
  }
}
```

## 2. SQL Server (LocalDB)

```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TreasureHuntDb;Trusted_Connection=true;"
  }
}
```

## 3. SQL Server (Full instance)

```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TreasureHuntDb;User Id=sa;Password=YourPassword123!;"
  }
}
```

## 4. MySQL

```json
{
  "DatabaseProvider": "MySQL",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TreasureHuntDb;Uid=root;Pwd=YourPassword;"
  }
}
```

## 5. In-Memory (Default, for testing)

```json
{
  "DatabaseProvider": "InMemory",
  "ConnectionStrings": {
    "DefaultConnection": ""
  }
}
```

## How to Use:

1. Copy the desired configuration to your `appsettings.json`
2. Update connection string with your actual database credentials
3. Run the application

## Database Creation:

- **SQLite**: File will be created automatically
- **SQL Server**: Database will be created automatically
- **MySQL**: Make sure MySQL server is running and accessible
- **In-Memory**: No setup required