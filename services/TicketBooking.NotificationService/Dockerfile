FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["services/TicketBooking.NotificationService/TicketBooking.NotificationService.csproj", "services/TicketBooking.NotificationService/"]
COPY ["shared/TicketBooking.Shared/TicketBooking.Shared.csproj", "shared/TicketBooking.Shared/"]
RUN dotnet restore "services/TicketBooking.NotificationService/TicketBooking.NotificationService.csproj"
COPY . .
WORKDIR "/src/services/TicketBooking.NotificationService"
RUN dotnet build "TicketBooking.NotificationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TicketBooking.NotificationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TicketBooking.NotificationService.dll"]
