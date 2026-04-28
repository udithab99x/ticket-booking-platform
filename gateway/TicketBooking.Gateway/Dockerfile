FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["gateway/TicketBooking.Gateway/TicketBooking.Gateway.csproj", "gateway/TicketBooking.Gateway/"]
RUN dotnet restore "gateway/TicketBooking.Gateway/TicketBooking.Gateway.csproj"
COPY . .
WORKDIR "/src/gateway/TicketBooking.Gateway"
RUN dotnet build "TicketBooking.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TicketBooking.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TicketBooking.Gateway.dll"]
