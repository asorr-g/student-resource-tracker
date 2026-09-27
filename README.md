# Student Academic Resource Tracker

A C# / ASP.NET Core (.NET 8) web app for tracking the study materials, assignments and
deadlines for each of your courses, with a dashboard that reports your progress.

## Features
- **Courses**: add / edit / delete (deleting a course removes its resources).
- **Resources**: lecture notes, textbooks, videos, articles, assignments, past questions, links.
  Each has a course, status (Not started / In progress / Completed), priority, due date, URL, tags and notes.
- **Filter and search** by course, type, status, priority, overdue-only, or free text (title, notes, tags).
- **Dashboard**: completion rate, overdue and due-this-week counts, progress per course,
  resources by type, open items by priority, and a list of upcoming deadlines.
- **CSV export** of all resources (opens cleanly in Excel).
- Data is stored in a local JSON file (`Data/tracker.json`), created with sample data on first run.

## Requirements
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A modern browser. Internet access is only needed once, for the automatic package restore.

Check your install: `dotnet --version`

## Run it
```bash
# 1. Unzip, then open a terminal in the project folder
cd StudentResourceTracker

# 2. Run
dotnet run
```
Then open the URL printed in the console (e.g. `http://localhost:5000`) in your browser.
To pick a port: `dotnet run --urls http://localhost:5080`

### Visual Studio / VS Code / Rider
Open `StudentResourceTracker.csproj` (or the folder) and press **F5** / Run.

## Project layout
```
StudentResourceTracker/
├── Program.cs                  # App startup + all REST endpoints
├── Models/
│   ├── Entities.cs             # Course, StudyResource, enums, DataFile
│   └── Dtos.cs                 # Request/response records
├── Services/
│   ├── DataStore.cs            # Thread-safe JSON persistence (atomic saves)
│   ├── AnalyticsService.cs     # Dashboard/report calculations
│   ├── Validators.cs           # Input validation
│   └── SeedData.cs             # First-run sample data
└── wwwroot/                    # Front end (HTML/CSS/vanilla JS)
```

## REST API
| Method | Route | Purpose |
|---|---|---|
| GET/POST | `/api/courses` | List / create courses |
| PUT/DELETE | `/api/courses/{id}` | Update / delete a course |
| GET | `/api/resources?courseId=&type=&status=&priority=&q=&overdue=` | List with filters |
| POST | `/api/resources` | Create a resource |
| PUT/DELETE | `/api/resources/{id}` | Update / delete a resource |
| PATCH | `/api/resources/{id}/status` | Change status only |
| GET | `/api/analytics/summary` | Dashboard figures |
| GET | `/api/export/csv` | CSV download |

## Configuration and tips
- **Reset the data**: stop the app and delete `Data/tracker.json`; sample data is recreated on next run.
- **Custom data file location**: `dotnet run --DataFile=/path/to/my-tracker.json`
- **Back up**: copy `Data/tracker.json`.
- **Extending**: to move to a real database, replace `DataStore` with an EF Core + SQLite implementation;
  the endpoints only talk to `DataStore.Read/Write`.

## Troubleshooting
- *"You must install .NET to run this application"*: install the .NET 8 SDK (link above).
- *Port already in use*: run with `--urls http://localhost:5080`.
