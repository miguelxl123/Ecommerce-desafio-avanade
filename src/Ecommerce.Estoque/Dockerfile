# Use the official .NET runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5002

# Use SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Ecommerce.Estoque/Ecommerce.Estoque.csproj", "src/Ecommerce.Estoque/"]
COPY ["src/Ecommerce.Shared/Ecommerce.Shared.csproj", "src/Ecommerce.Shared/"]
RUN dotnet restore "src/Ecommerce.Estoque/Ecommerce.Estoque.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/Ecommerce.Estoque"
RUN dotnet build "Ecommerce.Estoque.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Ecommerce.Estoque.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ecommerce.Estoque.dll"]