FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["services/TicketBooking.UserService/TicketBooking.UserService.csproj", "services/TicketBooking.UserService/"]
COPY ["shared/TicketBooking.Shared/TicketBooking.Shared.csproj", "shared/TicketBooking.Shared/"]
RUN dotnet restore "services/TicketBooking.UserService/TicketBooking.UserService.csproj"
COPY . .
WORKDIR "/src/services/TicketBooking.UserService"
RUN dotnet build "TicketBooking.UserService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TicketBooking.UserService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TicketBooking.UserService.dll"]
