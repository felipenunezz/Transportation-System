FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["DeviceSimulator/DeviceSimulator.csproj", "DeviceSimulator/"]
COPY ["Transportation_System/Transportation_System.csproj", "Transportation_System/"]
RUN dotnet restore "DeviceSimulator/DeviceSimulator.csproj"

COPY . .

WORKDIR "/src/DeviceSimulator"
RUN dotnet publish "./DeviceSimulator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DeviceSimulator.dll"]