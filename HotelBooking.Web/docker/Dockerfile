FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    NUGET_XMLDOC_MODE=skip

COPY Directory.Build.props ./
COPY HotelBooking.sln ./
COPY HotelBooking.Core/HotelBooking.Core.csproj HotelBooking.Core/
COPY HotelBooking.Application/HotelBooking.Application.csproj HotelBooking.Application/
COPY HotelBooking.Infrastructure/HotelBooking.Infrastructure.csproj HotelBooking.Infrastructure/
COPY HotelBooking.Web/HotelBooking.Web.csproj HotelBooking.Web/
COPY HotelBooking.Tests/HotelBooking.Tests.csproj HotelBooking.Tests/

RUN dotnet restore HotelBooking.sln

COPY . .
RUN dotnet publish HotelBooking.Web/HotelBooking.Web.csproj \
    --configuration Release \
    --no-restore \
    /p:UseAppHost=false \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080

RUN addgroup --system appgroup \
    && adduser --system --ingroup appgroup --home /home/appuser appuser \
    && mkdir -p /app/dpkeys \
    && chown -R appuser:appgroup /app

COPY --from=build /app/publish .
USER appuser

ENTRYPOINT ["dotnet", "HotelBooking.Web.dll"]
