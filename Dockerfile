FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

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
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HotelBooking.Web.dll"]
