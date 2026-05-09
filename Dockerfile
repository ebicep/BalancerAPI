FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BalancerAPI.sln .
COPY api/ api/
RUN dotnet restore api/1-Api/BalancerAPI.Api/BalancerAPI.Api.csproj
RUN dotnet publish api/1-Api/BalancerAPI.Api/BalancerAPI.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BalancerAPI.Api.dll"]
