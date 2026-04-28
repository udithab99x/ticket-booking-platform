FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["services/TicketBooking.EventService/TicketBooking.EventService.csproj", "services/TicketBooking.EventService/"]
COPY ["shared/TicketBooking.Shared/TicketBooking.Shared.csproj", "shared/TicketBooking.Shared/"]
RUN dotnet restore "services/TicketBooking.EventService/TicketBooking.EventService.csproj"
COPY . .
WORKDIR "/src/services/TicketBooking.EventService"
RUN dotnet build "TicketBooking.EventService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TicketBooking.EventService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TicketBooking.EventService.dll"]
