FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5081
EXPOSE 7169

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LeetCodeCompiler.API.csproj", "./"]
RUN dotnet restore "LeetCodeCompiler.API.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "LeetCodeCompiler.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LeetCodeCompiler.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
# Docker CLI talks to the host daemon via /var/run/docker.sock (mounted at runtime).
RUN apt-get update \
    && apt-get install -y --no-install-recommends docker.io \
    && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LeetCodeCompiler.API.dll"] 