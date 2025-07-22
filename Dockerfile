# SDK image (build uchun)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Proyekt fayllarini copy qilamiz
COPY . . 

# Publish (Release holatda)
RUN dotnet publish -c Release -o out

# Runtime image (ishlatish uchun engilroq)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Port ochish
EXPOSE 80

# Appni ishga tushurish
ENTRYPOINT ["dotnet", "SizningLoyihangiz.dll"]
