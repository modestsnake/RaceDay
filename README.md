# RaceDay API (Part 2)

RESTful ASP.NET Core Web API for the RaceDay event management system. Work is on the Part2-API branch.

## Setup and run
1. Install the .NET 10 SDK, SQL Server LocalDB and Git.
2. Clone the repo and switch to the Part2-API branch.
3. Check the connection string in RaceDay.Api/appsettings.json.
4. cd RaceDay.Api
5. dotnet ef database update
6. dotnet run, then open http://localhost:5287/swagger
7. Run the tests from the solution folder: dotnet test

## Roles
- **Organiser**: creates, edits and deletes events and categories, views enrolments, captures results. Registers with an organiser access code set in appsettings.json.
- **Participant**: browses events and categories, enrols in a category, views and updates their own profile, views their own results.

## Authentication
Cookie-based session. Passwords are hashed with the ASP.NET Core PasswordHasher. The role is stored in the session and enforced with Authorize attributes.
