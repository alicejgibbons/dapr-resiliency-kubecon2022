FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
WORKDIR "/src/CustomerLoyaltyJobNoLock"
RUN dotnet build "CustomerLoyaltyJobNoLock.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CustomerLoyaltyJobNoLock.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CustomerLoyaltyJobNoLock.dll"]
