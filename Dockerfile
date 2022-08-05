FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

ENV ASPNETCORE_ENVIRONMENT "Development"
ENV PORT = 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /src
COPY . .
RUN dotnet restore "BasicRedisLeaderboardDemoDotNetCore/BasicRedisLeaderboardDemoDotNetCore.csproj"

WORKDIR "/src/BasicRedisLeaderboardDemoDotNetCore"
RUN dotnet build "BasicRedisLeaderboardDemoDotNetCore.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "BasicRedisLeaderboardDemoDotNetCore.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
COPY --from=build /src/BasicRedisLeaderboardDemoDotNetCore/ClientApp/dist ./ClientApp/dist

ENTRYPOINT ["dotnet", "BasicRedisLeaderboardDemoDotNetCore.dll"]