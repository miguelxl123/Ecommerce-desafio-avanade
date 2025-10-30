# Use the official .NET runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5001

# Use SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Ecommerce.Vendas/Ecommerce.Vendas.csproj", "src/Ecommerce.Vendas/"]
COPY ["src/Ecommerce.Shared/Ecommerce.Shared.csproj", "src/Ecommerce.Shared/"]
RUN dotnet restore "src/Ecommerce.Vendas/Ecommerce.Vendas.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/Ecommerce.Vendas"
RUN dotnet build "Ecommerce.Vendas.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Ecommerce.Vendas.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ecommerce.Vendas.dll"]