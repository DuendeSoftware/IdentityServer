FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "hosts/main/Host.Main.csproj"
RUN dotnet build hosts/main/Host.Main.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish hosts/main/Host.Main.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Host.Main.dll"]