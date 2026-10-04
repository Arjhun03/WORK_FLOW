FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["SmartWorkflow.Api.csproj", "./"]
RUN dotnet restore "./SmartWorkflow.Api.csproj"

COPY . .
RUN dotnet build "SmartWorkflow.Api.csproj" -c Release -o /app/build
RUN dotnet publish "SmartWorkflow.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SmartWorkflow.Api.dll"]
