# RaceDay - Event Management System

RaceDay is a full-stack web platform for South African road running, walking and cycling events.
Organisers create events and capture results; Participants browse, enter events and track their race history.

## Roles and Features

**Organiser:** dashboard (events, enrolments per event, upcoming dates), create/edit/delete events with banner upload (Azure Blob), manage categories (distance or age), view enrolments with status, capture and publish finish times and positions.

**Participant:** home page with filtering (name, type, city, date), event detail with categories, enrol in a category, My Events (with colour-coded status: green = Confirmed, amber = Pending, red = Cancelled), My Results (e.g. "47th out of 312 finishers"), profile with picture upload.

**Security:** session-based authentication, role-based navigation, protected pages redirect unauthenticated users to Login, logout clears the session.

## Architecture

```
Browser -> Part3-MVC/RaceDayWeb (ASP.NET Core MVC, no database access)
              -> Part2-API/RaceDayApi (REST API, session auth, EF Core)
                    -> SQL Server
                    -> Azure Blob Storage (banners and profile pictures)
```
The MVC app only talks to the API. Azure is only used by the API.

## Repository Structure

```
raceday-system/
  docs/                  Part 1 and Part 2 documents, plus part3/ screenshots
  Part2-API/             REST API + unit tests (Dockerfile inside RaceDayApi/)
  Part3-MVC/RaceDayWeb/  MVC front end (Dockerfile inside)
  docker-compose.yml     Runs SQL Server + API + MVC
  .env.example           Settings template
  .github/workflows/     CI workflows
```

## Run with Docker (single command)

1. Install Docker Desktop.
2. Clone and configure:
```
git clone https://github.com/St10456708/raceday-system.git
cd raceday-system
copy .env.example .env
```
3. Open `.env` and paste your Azure Storage connection string into `AZURE_BLOB_CONNECTION_STRING` (container `raceday-images`, public access level "Blob").
4. Start everything:
```
docker compose up --build
```
5. Open the app: http://localhost:8080 (API Swagger: http://localhost:5018/swagger).
6. Register one Organiser and one Participant to try both interfaces. The database is created automatically.

Stop with `Ctrl+C`; remove containers and data with `docker compose down -v`.

## Azure Blob Storage Setup

Storage account -> Container `raceday-images` -> Anonymous access level: Blob. Copy the connection string (Access keys) into `.env`.
Uploads go MVC -> API (multipart) -> Azure Blob. The public blob URL is stored in the database and displayed by the MVC app.

## Continuous Integration

GitHub Actions (`.github/workflows/part3-build.yml`) validates the repository structure, builds and tests the API, builds the MVC project and builds the Docker images on every push.

![CI success Part 3](docs/part3/ci-success-part3.png)

## Screenshots

![Dashboard](docs/part3/organiser dashboard.png)
![Event detail](docs/part3/event detail.png)
![My results](docs/part3/my results.png)

## Video Walkthrough

Part 3 demo (unlisted): [YouTube link](PASTE-YOUTUBE-LINK-HERE)

Part 1 planning video: [YouTube link](https://youtu.be/VwANrvkZYzs)
