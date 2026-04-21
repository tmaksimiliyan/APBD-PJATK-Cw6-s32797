# Cwiczenia6 - REST API dla wizyt w przychodni z ADO.NET

Projekt ASP.NET Core Web API przygotowany w ramach przedmiotu APBD.  
Aplikacja obsługuje wizyty w przychodni i komunikuje się z bazą danych SQL Server przy użyciu **ADO.NET**, bez użycia Entity Framework.

## Technologies
- C#
- ASP.NET Core Web API
- ADO.NET
- Microsoft.Data.SqlClient
- SQL Server
- Docker
- Swagger / Swashbuckle

## Features
API udostępnia operacje CRUD dla wizyt:
- `GET /api/Appointments` – lista wizyt z opcjonalnym filtrowaniem po `status` i `patientLastName`
- `GET /api/Appointments/{idAppointment}` – szczegóły jednej wizyty
- `POST /api/Appointments` – dodanie nowej wizyty
- `PUT /api/Appointments/{idAppointment}` – aktualizacja wizyty
- `DELETE /api/appointments/{idAppointment}` – usunięcie wizyty

## Database
Projekt korzysta z bazy danych **ClinicAdoNet** uruchomionej na **SQL Server w kontenerze Docker**.  
Serwer bazy został postawiony lokalnie przy użyciu obrazu Microsoft SQL Server, a następnie baza została utworzona i zasilona danymi testowymi przy pomocy dostarczonego skryptu SQL.

Komunikacja z bazą została zaimplementowana ręcznie przez:
- `SqlConnection`
- `SqlCommand`
- `SqlDataReader`
- parametry SQL
