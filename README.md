# Basic Redis Leaderboard Demo .Net 6 with Write-Behind

## Summary

We based this project from our [Basic Leaderboard](https://github.com/redis-developer/basic-redis-leaderboard-demo-dotnet) project to show how you can acess Azure Cache for Redis using .NET 6 and added a Write-Behind pattern to Azure SQL. At the moment, our preferred way to implement this pattern is to use RedisGears, but is not availble in ACRE at this time.

We decided to implement the Write-Behind pattern using an Azure Function that reads the key change sent through a Redis Stream. It is using a pulling mechanism but we are looking forward to implement it using an event-driven approach.

![How it works](./Solution%20Items/Images/screenshot001.png)

## Features

- Listens to Key Space Notifications to add changes to the stream
- Use StackExchange.Redis to access ACRE
- Use Azure Function to sync the updates to Azure SQL db using a Write-Behind pattern

## Architecture
![Architecture](/Solution%20Items/Images/architecture.png)
## Prerequisites

- VS Code or Visual Studio
- .Net 6
- OSX or Windows
- Azure SQL
  - Configuration steps [here](https://learn.microsoft.com/en-us/azure/azure-sql/database/single-database-create-quickstart?view=azuresql&tabs=azure-portal)
- Azure Cache for Redis Enterprise
  - Configuration steps [here](https://learn.microsoft.com/en-us/azure/azure-cache-for-redis/quickstart-create-redis-enterprise)

## Installation

### Azure SQL
Run SQL Script to create table:

1. Copy the contents of the script named "CreateCompanyTable.sql". The file is located inside the SQL folder located in the Solution Items folder.

2. Open the query editor of your preferred database tool (Azure Data Studio or SQL Server Management Studio) and paste the SQL script copied during Step 1.

3. Run the script to create the table.

### Front End
If you need to run the front end by itself:

1. Go to the ClientApp folder

```sh
cd BasicRedisLeaderboardDemoDotNetCore
cd ClientApp
code .
```

2. Install node modules

```sh
npm install
```

3. Run front end

```sh
npm run serve
```

## Quickstart

1. Clone the git repository

```sh
git clone https://github.com/Redislabs-Solution-Architects/acre-sql-demo
```

2. Open it with your Visual Studio Code or Visual Studio

3. Update App Settings to: include actual connection to Redis, Azure SQL and configure the application:

```text
RedisHost = "Redis server URI"
RedisPort = "Redis port"
RedisPassword = "Password to the server"
IsACRE = "True if using Azure Cache for Redis Enterprise"
AllowAdmin = "True if need to run certain commands"
DeleteAllKeysOnLoad = "True if need to delete all keys during load"
LoadInitialData = "True if running the application for the first time and want to load test data"
UseReadThrough = "True to use the Read Through pattern"
UseWriteBehind = "True to use the Write Behind pattern"
ReadThroughFunctionBaseUrl = "Url of the Read Through Function"
```

4. Update local.settings.json for the SQLSweeperFunction
    - Replace "--SECRET--" with the real connection strings for Azure SQL and Redis

    ```text
    "ConnectionStrings": {
      "SQLConnectionString": "--SECRET--",
      "RedisConnectionString": "--SECRET--"
    }
    ```

5. Update local.settings.json for the ReaderFunction
    - Replace "--SECRET--" with the real values

    ```text
    "ReaderFunctionSettings:RedisHost": "--SECRET--",
    "ReaderFunctionSettings:RedisPort": "10000",
    "ReaderFunctionSettings:RedisPassword": "--SECRET--",
    "ReaderFunctionSettings:IsACRE": "true",
    "ReaderFunctionSettings:SQLConnectionString": "--SECRET--"
    ```

6. Run backend

```sh
dotnet run
```

7. Run Azure Function (Write Behind)
    - You can try the Write BEhind pattern by setting "true" to the "UseWriteBehind" configuration variable inside the appsettings.json. If so, you need to run the Write Behind Function by:

    ```sh
    cd SQLSweeperFunction
    func start
    ```

8. Run Azure Function (Read Through)
   - You can try the Read Through pattern by setting "true" to the "UseReadThrough" configuration variable inside the appsettings.json. If so, you need to run the Read Through function by:

   ```sh
   cd ReaderFunction
   func start
   ```

Note:
Static content runs automatically with the backend part. In case you need to run it separately, please see README in the [client](./BasicRedisLeaderboardDemoDotNetCore/ClientApp/README.md) folder.

## Demo

#### Deploy to Azure

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FRedislabs-Solution-Architects%2Facre-sql-demo%2Fmain%2FSolution%20Items%2FAzure%2Farm%2Fazuredeploy.json)

## Resources
- [Basic Readis Leaderboard Demo](https://github.com/redis-developer/basic-redis-leaderboard-demo-dotnet)
