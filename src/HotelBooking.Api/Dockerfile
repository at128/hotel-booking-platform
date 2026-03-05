# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src


COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]

COPY ["HotelBooking.sln", "."]
COPY ["src/HotelBooking.Api/HotelBooking.Api.csproj", "src/HotelBooking.Api/"]
COPY ["src/HotelBooking.Application/HotelBooking.Application.csproj", "src/HotelBooking.Application/"]
COPY ["src/HotelBooking.Infrastructure/HotelBooking.Infrastructure.csproj", "src/HotelBooking.Infrastructure/"]
COPY ["src/HotelBooking.Domain/HotelBooking.Domain.csproj", "src/HotelBooking.Domain/"]
COPY ["src/HotelBooking.Contracts/HotelBooking.Contracts.csproj", "src/HotelBooking.Contracts/"]

RUN dotnet restore "src/HotelBooking.Api/HotelBooking.Api.csproj"

COPY . .
WORKDIR "/src/src/HotelBooking.Api"
RUN dotnet build "HotelBooking.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "HotelBooking.Api.csproj" -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN addgroup --system --gid 1001 appgroup \
    && adduser --system --uid 1001 --ingroup appgroup --no-create-home appuser

COPY --from=publish /app/publish .
RUN mkdir -p /app/wwwroot/images \
    && chown -R appuser:appgroup /app/wwwroot
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "HotelBooking.Api.dll"]