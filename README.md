# CurrencyTransferAPI

## Opis Projektu

CurrencyTransferAPI to backend REST API obsługujący operacje związane z transferem środków pieniężnych, zarządzaniem kontami walutowymi użytkowników, wymianą walut oraz uwierzytelnianiem i autoryzacją użytkowników. API integruje się z zewnętrznym serwisem NBP w celu pobierania aktualnych kursów walut.

## Technologie

* **.NET 7** (lub nowszy/starszy w zależności od Twojej wersji)
* **ASP.NET Core**
* **Entity Framework Core**
* **MySQL** (jako baza danych)
* **JWT (JSON Web Tokens)** do uwierzytelniania i autoryzacji
* **Swagger (OpenAPI)** do dokumentacji API
* **Serilog** (lub inny system logowania, sądząc po `ILogger`)
* Integracja z API Narodowego Banku Polskiego (NBP)

## Główne Funkcjonalności

* **Uwierzytelnianie i Autoryzacja:**
    * Rejestracja nowych użytkowników (`POST /api/Auth/register`)
    * Logowanie użytkowników i wydawanie tokenów JWT (`POST /api/Auth/login`)
    * Pobieranie danych o zalogowanym użytkowniku (`GET /api/Auth/me`)
    * (Opcjonalnie) Dostęp do strefy administracyjnej (`GET /api/Auth/adminarea` - wymaga odpowiedniej roli)
* **Zarządzanie Kontami Walutowymi:**
    * Tworzenie nowych kont walutowych dla użytkownika (`POST /api/Accounts`)
    * Pobieranie listy kont zalogowanego użytkownika (`GET /api/Accounts`)
    * Pobieranie szczegółów konkretnego konta (`GET /api/Accounts/{accountId}`)
* **Transfery Środków:**
    * Wykonywanie przelewów między kontami (`POST /api/Transfers`)
    * Pobieranie historii transakcji użytkownika (`GET /api/Transfers` - zaimplementowane)
* **Operacje Walutowe:**
    * Konwersja kwot między walutami na podstawie kursów NBP (`POST /api/Currency/convert`)
    * Wymiana walut między kontami użytkownika (`POST /api/Exchange/perform`)
* **Narzędzia (Utils):**
    * Pobieranie listy dozwolonych/obsługiwanych kodów walut (`GET /api/Utils/allowed-currencies`)

## Struktura Projektu (Główne Katalogi)

* `Controllers/` - Kontrolery API obsługujące żądania HTTP.
* `Services/` - Serwisy zawierające logikę biznesową.
* `Data/` - Kontekst bazy danych (`ApplicationDbContext`).
* `Models/` - Modele encji bazy danych.
* `DTOs/` (lub w `Services`/`Models`) - Obiekty Transferu Danych używane w API.
* `Migrations/` - Migracje Entity Framework Core.

## Konfiguracja i Uruchomienie

### Wymagania Wstępne

* [.NET SDK](https://dotnet.microsoft.com/download) (wersja zgodna z projektem, np. .NET 7.0)
* Serwer MySQL
* Opcjonalnie: Narzędzie do zarządzania bazą danych MySQL (np. DBeaver, MySQL Workbench)

### Konfiguracja

1.  **Baza Danych:**
    * Utwórz bazę danych MySQL.
    * Skonfiguruj string połączeniowy `DefaultConnection` в `appsettings.Development.json` (lub `appsettings.json`):
        ```json
        {
          "ConnectionStrings": {
            "DefaultConnection": "server=TWOJ_SERWER;port=3306;database=NAZWA_TWOJEJ_BAZY;user=TWOJ_UZYTKOWNIK;password=TWOJE_HASLO"
          },
          // ...
        }
        ```
2.  **JWT:**
    * Skonfiguruj klucz, wystawcę i audytorium dla JWT w `appsettings.Development.json` (lub `appsettings.json`):
        ```json
        {
          "Jwt": {
            "Key": "TWOJ_BARDZO_DŁUGI_I_SEKRETNY_KLUCZ_JWT", // Minimum 32 znaki dla HS256
            "Issuer": "TWOJ_WYSTAWCA", // np. https://localhost:5001
            "Audience": "TWOJE_AUDYTORIUM" // np. https://localhost:5001 lub nazwa aplikacji
          },
          // ...
        }
        ```

### Uruchomienie Projektu

1.  Sklonuj repozytorium (jeśli dotyczy).
2.  Otwórz terminal w głównym katalogu projektu backendu.
3.  Przywróć zależności:
    ```bash
    dotnet restore
    ```
4.  Zastosuj migracje do bazy danych (jeśli baza jest pusta lub wymaga aktualizacji):
    ```bash
    dotnet ef database update
    ```
5.  Uruchom aplikację:
    ```bash
    dotnet run
    ```
    API będzie domyślnie dostępne pod adresem `http://localhost:5033` (lub innym skonfigurowanym w `launchSettings.json`).

## Dokumentacja API (Swagger)

Po uruchomieniu aplikacji, interaktywna dokumentacja API (Swagger UI) jest dostępna pod adresem:
`http://localhost:5033/swagger`

## Główne Punkty Końcowe API (Przykłady)

* `POST /api/Auth/register` - Rejestracja użytkownika
* `POST /api/Auth/login` - Logowanie użytkownika
* `GET /api/Auth/me` - Informacje o zalogowanym użytkowniku
* `POST /api/Accounts` - Tworzenie nowego konta walutowego
* `GET /api/Accounts` - Lista kont zalogowanego użytkownika
* `POST /api/Transfers` - Wykonanie przelewu
* `GET /api/Transfers` - Historia transakcji użytkownika
* `POST /api/Currency/convert` - Konwersja walut
* `GET /api/Utils/allowed-currencies` - Lista obsługiwanych walut

## Odniesienie do Wymagań Projektowych

Projekt stara się spełniać następujące wymagania:
* Komunikacja HTTP (GET, POST; PUT/DELETE do rozważenia)
* Obsługa kodów błędów HTTP (400, 401, 404, 405, 500; 403 przy autoryzacji opartej na rolach)
* Integracja z zewnętrznym API (NBP)
* Uwierzytelnianie i autoryzacja JWT
* Dokumentacja OpenAPI (Swagger)

## TODO / Możliwe Rozszerzenia

* Implementacja metod `PUT` (np. do aktualizacji profilu użytkownika) i `DELETE` (np. do usuwania konta z zerowym saldem, z uwzględnieniem zasad biznesowych).
* Dodanie drugiej integracji z zewnętrznym API (np. system płatności).
* Pełne wdrożenie autoryzacji opartej na rolach (np. dla panelu administracyjnego).
* Napisanie testów jednostkowych i integracyjnych.
* Obsługa przelewów międzywalutowych w `TransferService` (obecnie wspiera tylko przelewy w tej samej walucie).
* Bardziej szczegółowe DTO dla odpowiedzi API, aby unikać bezpośredniego zwracania modeli encji.
