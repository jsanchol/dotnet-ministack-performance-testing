FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "LambdaApp/LambdaApp.csproj"
WORKDIR /src/LambdaApp
RUN dotnet build "LambdaApp.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR /src/LambdaApp
RUN dotnet publish "LambdaApp.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LambdaApp.dll"]
