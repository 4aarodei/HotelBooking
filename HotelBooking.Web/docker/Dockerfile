FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY NuGet.Config ./

COPY HotelBooking.Domain/HotelBooking.Domain.csproj HotelBooking.Domain/
COPY HotelBooking.Application/HotelBooking.Application.csproj HotelBooking.Application/
COPY HotelBooking.Infrastructure/HotelBooking.Infrastructure.csproj HotelBooking.Infrastructure/
COPY HotelBooking.Web/HotelBooking.Web.csproj HotelBooking.Web/

RUN dotnet restore HotelBooking.Web/HotelBooking.Web.csproj /p:DisableImplicitNuGetFallbackFolder=true

COPY . .

RUN dotnet publish HotelBooking.Web/HotelBooking.Web.csproj \
    --configuration Release \
    --no-restore \
    /p:DisableImplicitNuGetFallbackFolder=true \
    /p:UseAppHost=false \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HotelBooking.Web.dll"]
