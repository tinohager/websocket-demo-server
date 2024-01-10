FROM mcr.microsoft.com/dotnet/aspnet:8.0.0-jammy-chiseled-amd64

# Listen ports
EXPOSE 8080

# Copy files from other build jobs
COPY publish .

ENTRYPOINT ["dotnet", "WebsocketDemoServer.dll"]