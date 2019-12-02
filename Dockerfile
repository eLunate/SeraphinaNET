FROM mcr.microsoft.com/dotnet/core/runtime:3.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["SeraphinaNET.csproj", ""]
RUN dotnet restore "./SeraphinaNET.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "SeraphinaNET.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SeraphinaNET.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SeraphinaNET.dll"]