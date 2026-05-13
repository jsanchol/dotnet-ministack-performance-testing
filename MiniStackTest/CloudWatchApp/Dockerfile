# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "CloudWatchApp/CloudWatchApp.csproj"
WORKDIR /src/CloudWatchApp
RUN dotnet build "CloudWatchApp.csproj" -c Release -o /app/build /p:GenerateAssemblyInfo=false

FROM build AS publish
WORKDIR /src/CloudWatchApp
RUN dotnet publish "CloudWatchApp.csproj" -c Release -o /app/publish

# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CloudWatchApp.dll"]