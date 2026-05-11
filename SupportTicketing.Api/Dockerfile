FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV AllowedHosts=*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SupportTicketing.Api/SupportTicketing.Api.csproj", "SupportTicketing.Api/"]
COPY ["SupportTicketing.Core/SupportTicketing.Core.csproj", "SupportTicketing.Core/"]
COPY ["SupportTicketing.Infrastructure/SupportTicketing.Infrastructure.csproj", "SupportTicketing.Infrastructure/"]
RUN dotnet restore "SupportTicketing.Api/SupportTicketing.Api.csproj"
COPY . .
WORKDIR "/src/SupportTicketing.Api"
RUN dotnet build "SupportTicketing.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SupportTicketing.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SupportTicketing.Api.dll"]
