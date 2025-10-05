FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LiveSupportDashboard/LiveSupportDashboard.csproj", "LiveSupportDashboard/"]
COPY ["LiveSupportDashboard.Domain/LiveSupportDashboard.Domain.csproj", "LiveSupportDashboard.Domain/"]
RUN dotnet restore "LiveSupportDashboard/LiveSupportDashboard.csproj"
COPY . .
WORKDIR "/src/LiveSupportDashboard"
RUN dotnet build "./LiveSupportDashboard.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LiveSupportDashboard.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LiveSupportDashboard.dll"]
