FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /src
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV AllowedHosts=*
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY ["SupportTicketing.Api/SupportTicketing.Api.csproj", "SupportTicketing.Api/"]
COPY ["SupportTicketing.Core/SupportTicketing.Core.csproj", "SupportTicketing.Core/"]
COPY ["SupportTicketing.Infrastructure/SupportTicketing.Infrastructure.csproj", "SupportTicketing.Infrastructure/"]
RUN dotnet restore "SupportTicketing.Api/SupportTicketing.Api.csproj"
COPY . .
WORKDIR "/src/SupportTicketing.Api"
ENTRYPOINT ["dotnet", "run", "--no-launch-profile", "-c", "Release"]
