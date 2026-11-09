# Nutrivo

A calorie and nutrition tracking app built with .NET 10, DDD, Clean Architecture,
PostgreSQL and React Native. The backend provides food management and daily diary
entries with nutrition totals. The mobile catalogue uses the API; its diary and
targets currently use local examples/session state.

## Prerequisites

- .NET 10 SDK.
- Docker with Docker Compose, with the Docker engine running.
- JetBrains Rider for backend debugging.
- For the mobile app: Node.js, npm and Expo Go; see the
  [mobile setup guide](mobile/nutrivo-app/README.md).

## Local setup

After cloning the repository, run these commands from the repository root.
The API runs locally; PostgreSQL runs in Docker.

### 1. Start PostgreSQL

```sh
cp .env.example .env
```

In `.env`, replace `POSTGRES_PASSWORD` with a local password. The default database
and username are `nutrivo`; change `POSTGRES_PORT` if port 5432 is already in use.

```sh
docker compose up -d --wait
```

### 2. Configure the API connection

Set the same database, username, password and host port as in `.env`:

```sh
dotnet user-secrets set 'ConnectionStrings:Nutrivo' \
  'Host=localhost;Port=5432;Database=nutrivo;Username=nutrivo;Password=YOUR_LOCAL_PASSWORD' \
  --project src/Nutrivo.Api
```

Docker Compose reads `.env`; **ASP.NET Core does not**. User Secrets are loaded
in Development and stored outside the repository. Keep local credentials out of
Git. Changing `.env` does not change credentials in an existing database volume;
see [database configuration](docs/development.md#local-configuration-and-database-lifecycle).

### 3. Restore dependencies and apply migrations

```sh
dotnet restore
dotnet tool restore
dotnet ef database update \
  --project src/Nutrivo.Infrastructure \
  --startup-project src/Nutrivo.Api \
  -- --environment Development
```

**Starting the API does not apply migrations.** Run the database update again
after pulling changes that add migrations.

## Debug the API in Rider

1. Open `Nutrivo.slnx` and let Rider load the projects.
2. Select the launch configuration for `Nutrivo.Api` with the `http` profile from
   `src/Nutrivo.Api/Properties/launchSettings.json`. If it is missing, right-click
   that file and choose **Generate Configurations**, then select `http`.
3. Place a breakpoint on the `await handler.HandleAsync(...)` line in
   `src/Nutrivo.Api/Endpoints/Foods/SearchFoods/SearchFoodsEndpoint.cs` and start
   the configuration with **Debug**.
4. Open [Swagger UI](http://localhost:5097/swagger), expand **GET /foods** and use
   **Try it out → Execute**. Rider should pause at the breakpoint, even when the
   database contains no foods. Resume execution to see the response.

The `http` profile selects Development. Swagger UI and `/openapi/v1.json` are
available only in that environment; the browser does not open automatically.
See [Rider launch profiles](https://www.jetbrains.com/help/rider/Running_LaunchSettings.html)
for configuration details.

To run from a terminal instead, use:

```sh
dotnet run --project src/Nutrivo.Api --launch-profile http
```

Run one API instance at a time to avoid port conflicts. To connect a phone and
keep backend breakpoints active, follow the [mobile setup guide](mobile/nutrivo-app/README.md).

## Common commands

Run from the repository root:

| Command | Purpose |
| --- | --- |
| `dotnet build` | Build the backend. |
| `dotnet test` | Run backend tests; integration tests require Docker. |
| `docker compose down` | Stop PostgreSQL and preserve local data. |
| `docker compose up -d --wait` | Restart PostgreSQL. |

## Documentation

- [Mobile setup and debugging](mobile/nutrivo-app/README.md)
- [API examples and contracts](docs/api.md)
- [Migrations, formatting and backend validation](docs/development.md)
- [Mobile design, troubleshooting and validation](docs/mobile.md)
- [Architecture decisions](docs/adr/)
