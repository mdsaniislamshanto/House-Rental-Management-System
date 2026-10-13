FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["Shanto VilaManagement System/Shanto VilaManagement System.csproj", "Shanto VilaManagement System/"]
RUN dotnet restore "Shanto VilaManagement System/Shanto VilaManagement System.csproj"

# Copy all source files and publish release
COPY . .
WORKDIR "/src/Shanto VilaManagement System"
RUN dotnet publish "Shanto VilaManagement System.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Shanto VilaManagement System.dll"]
