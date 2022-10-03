# Basic Redis Leaderboard Demo .Net 6 with Write-Behind

## Summary

We based this project from our [Basic Leaderboard](https://github.com/redis-developer/basic-redis-leaderboard-demo-dotnet) project to show how you can acess Azure Cache for Redis using .NET 6 and added a Write-Behind pattern to Azure SQL. At the moment, our preferred way to implement this pattern is to use RedisGears, but is not availble in ACRE at this time.

We decided to implement the Write-Behind pattern using an Azure Function that reads the key change sent through a Redis Stream. It is using a pulling mechanism but we are looking forward to implement it using an event-driven approach.

![How it works](./Solution%20Items/Images/screenshot001.png)

## Features

- Listens to Key Space Notifications to add changes to the stream
- Use StackExchange.Redis to access ACRE
- Use Azure Function to sync the updates to Azure SQL db using a Write-Behind pattern

### Architecture
![Architecture](/Solution%20Items/Images/architecture.png)
### Prerequisites

- VS Code or Visual Studio
- .Net 6
- OSX or Windows

### Installation

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

### Quickstart

1. Clone the git repository

```sh
git clone https://github.com/Redislabs-Solution-Architects/acre-sql-demo
```

2. Open it with your Visual Studio Code or Visual Studio
3. Update App Settings to include actual connection to Redis:

```text
RedisHost = "Redis server URI"
RedisPort = "Redis port"
RedisPassword = "Password to the server"
IsACRE = "True if using Azure Cache for Redis Enterprise"
AllowAdmin = "True if need to run certain commands"
DeleteAllKeysOnLoad = "True if need to delete all keys during load"
```

4. Run backend

```sh
dotnet run
```

5. Run Azure Function

```sh
cd SQLSweeperFunction
func start
```

Note:
Static content runs automatically with the backend part. In case you need to run it separately, please see README in the [client](./BasicRedisLeaderboardDemoDotNetCore/ClientApp/README.md) folder.

## Demo

#### Deploy to Heroku

<p>
    <a href="https://heroku.com/deploy" target="_blank">
        <img src="https://www.herokucdn.com/deploy/button.svg" alt="Deploy to Heorku" />
    </a>
</p>

#### Deploy to Google Cloud

<p>
    <a href="https://deploy.cloud.run" target="_blank">
        <img src="https://deploy.cloud.run/button.svg" alt="Run on Google Cloud" width="150px"/>
    </a>
</p>

#### Deploy to Azure

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FRedislabs-Solution-Architects%2Facre-sql-demo%2Fmain%2FSolution%20Items%2FAzure%2Farm%2Fazuredeploy.json)

## Resources
- [Basic Readis Leaderboard Demo](https://github.com/redis-developer/basic-redis-leaderboard-demo-dotnet)
