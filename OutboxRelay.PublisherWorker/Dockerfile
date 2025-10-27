
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


COPY ../../OutboxRelay.sln .
COPY src/OutboxRelay.Api/OutboxRelay.Api.csproj src/OutboxRelay.Api/
COPY src/OutboxRelay.Application/OutboxRelay.Application.csproj src/OutboxRelay.Application/
COPY src/OutboxRelay.Common/OutboxRelay.Common.csproj src/OutboxRelay.Common/
COPY src/OutboxRelay.ConsumerWorkerService/OutboxRelay.ConsumerWorkerService.csproj src/OutboxRelay.ConsumerWorkerService/
COPY src/OutboxRelay.Core/OutboxRelay.Core.csproj src/OutboxRelay.Core/
COPY src/OutboxRelay.Infrastructure/OutboxRelay.Infrastructure.csproj src/OutboxRelay.Infrastructure/
COPY src/OutboxRelay.PublisherWorkerService/OutboxRelay.PublisherWorkerService.csproj src/OutboxRelay.PublisherWorkerService/

RUN dotnet restore "src/OutboxRelay.Api/OutboxRelay.Api.csproj"

COPY . .
WORKDIR "/src/src/OutboxRelay.Api"
RUN dotnet build "OutboxRelay.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OutboxRelay.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OutboxRelay.PublisherWorkerService.dll"]