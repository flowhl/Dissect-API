# Use the custom OpenCVSharp runtime as a parent image
FROM ghcr.io/shimat/opencvsharp/ubuntu20-dotnet6sdk-opencv4.6.0:20220615 AS base
WORKDIR /app
EXPOSE 5000

# Use the same image for the SDK as it should contain the necessary tools
FROM ghcr.io/shimat/opencvsharp/ubuntu20-dotnet6sdk-opencv4.6.0:20220615 AS build
WORKDIR /src
COPY ["DissectAPI/DissectAPI.csproj", "./DissectAPI/"]
RUN dotnet restore "DissectAPI/DissectAPI.csproj"
COPY . .
WORKDIR "/src/DissectAPI"
RUN dotnet build "DissectAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DissectAPI.csproj" -c Release -o /app/publish

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN chmod +x /app/External/Linux/r6-dissect

ENTRYPOINT ["dotnet", "DissectAPI.dll", "--urls", "http://*:5000"]
