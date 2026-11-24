# EventTicketing

מערכת לניהול אירועים והזמנת מקומות ישיבה.

המערכת מאפשרת למשתמשים לצפות באירועים עתידיים,
לבחור מקומות פנויים, לבצע הזמנות ולנהל את היסטוריית ההזמנות שלהם.

מנהלי המערכת יכולים לנהל אולמות, אירועים ומקומות שמורים.

## Technologies

- ASP.NET Core Web API
- .NET 10
- C#
- Entity Framework Core
- PostgreSQL
- Npgsql
- AutoMapper
- JWT Authentication
- NLog
- Swagger
- xUnit
- Moq

## Architecture

המערכת בנויה בארכיטקטורת ארבע שכבות:

| Project | Responsibility |
|---|---|
| EventTicketing.Core | Entities, DTOs, Enums, Interfaces, Exceptions |
| EventTicketing.Data | DbContext, Repositories, Migrations, Unit of Work |
| EventTicketing.Service | Business Logic, Validation, Mapping |
| EventTicketing.API | Controllers, Middleware, Authentication, DI |

בנוסף קיים פרויקט EventTicketing.Tests עבור בדיקות אוטומטיות.

### Project Dependencies

- Core אינו תלוי בפרויקטים אחרים.
- Data תלוי ב-Core.
- Service תלוי ב-Core.
- API משמש כנקודת הכניסה וכ-Composition Root.

התלויות מוזרקות באמצעות Dependency Injection.
ה-DbContext, ה-Repositories והשירותים רשומים כ-Scoped.

## Database

המערכת משתמשת ב-PostgreSQL ובגישת EF Core Code First.

### Entities

- User
- Hall
- Seat
- Event
- EventSeat
- Booking

EventSeat מייצגת מקום ישיבה מסוים באירוע מסוים,
ומקשרת בין Event לבין Seat.

מסד הנתונים כולל קשרים בין ישויות, מפתחות זרים,
אינדקסים ואילוצי ייחודיות.

קיימות שתי מיגרציות:

1. InitialCreate
2. AddEventStatusStartTimeIndex

## Authentication and Authorization

המערכת משתמשת ב-JWT לצורך אימות משתמשים.

הסיסמאות נשמרות באמצעות Password Hashing מסוג PBKDF2,
ולא כטקסט גלוי.

### User

- צפייה באירועים ובמקומות ישיבה.
- הזמנת מקום פנוי.
- ביטול הזמנה בהתאם למדיניות המערכת.
- צפייה בהיסטוריית ההזמנות האישית.

### Admin

- יצירה, עדכון ומחיקה של אולמות בהתאם לכללים העסקיים.
- יצירה ועדכון של אירועים.
- ביטול ושחזור אירועים.
- שמירה ושחרור של מקומות ישיבה.
- צפייה באירועים בכל הסטטוסים.

חשבון מנהל נוצר באמצעות Seed Data בלבד.

## Business Rules

- רשימת האירועים הציבורית כוללת אירועים פעילים ועתידיים בלבד.
- רשימת האירועים מחולקת לעמודים של 10 אירועים.
- יצירת אירוע מותרת לפחות 24 שעות לפני תחילתו.
- אירוע מתחיל ומסתיים באותו יום.
- שעת תחילת אירוע אינה מוקדמת מ-08:00.
- בין אירועים באותו אולם נדרש מרווח של שעה לפחות.
- ניתן לערוך אירוע עד חצות שלפני יום האירוע.
- לאחר שבוצעה הזמנת משתמש לאירוע, לא ניתן לשנות
  את האולם, התאריך או שעות האירוע.
- כל פעולת הזמנה כוללת מקום ישיבה אחד.
- הזמנה מותרת עד תחילת האירוע.
- ביטול הזמנה מותר עד שעה לפני תחילת האירוע.
- ביטול הזמנה נשמר באמצעות Soft Delete.
- ביטול אירוע מבטל את הזמנות המשתמשים המשויכות אליו.
- שחזור אירוע אינו משחזר הזמנות משתמשים שבוטלו.
- מקומות שנשמרו על ידי המנהל נשארים שמורים
  בעת ביטול ושחזור אירוע.
- שחזור אירוע מותר עד חצות שלפני יום האירוע.
- מספר המקומות באולם נקבע בעת יצירתו.
- לא ניתן למחוק אולם שכבר שימש לאירוע.

## Concurrency and Transactions

המשאב המוגבל במערכת הוא מקום ישיבה באירוע,
המיוצג באמצעות הישות EventSeat.

מספר משתמשים עשויים לנסות להזמין את אותו מקום במקביל.
ללא מנגנון הגנה עלולה להיווצר הזמנה כפולה.

המערכת משלבת שני מנגנוני הגנה:

### Optimistic Concurrency

לישות EventSeat מוגדר שדה Version מסוג Guid
המשמש כ-Concurrency Token ב-Entity Framework Core.

בעת שינוי מקום ישיבה מתעדכן ערך ה-Version.

כאשר פעולה מנסה לשמור שינוי על סמך גרסה ישנה,
EF Core עשוי לזרוק DbUpdateConcurrencyException.

### Serializable Transactions

פעולות עסקיות התלויות במצב הנתונים מבוצעות
בתוך טרנזקציות ברמת בידוד Serializable.

הבדיקות העסקיות ושמירת השינויים מתבצעות
במסגרת אותה טרנזקציה.

ניהול הטרנזקציות מרוכז ב-Unit of Work,
הכולל Commit, Rollback ותמיכה ב-Savepoints.

במקרה של כשל, השינויים שבוצעו במסגרת הפעולה מבוטלים.

### Conflict Response

במקרה של התנגשות עסקית, Optimistic Concurrency,
Serialization Failure או Deadlock,
המערכת מחזירה HTTP 409 Conflict עם הודעה מתאימה.

הטיפול בחריגות מרוכז ב-ExceptionHandlingMiddleware.

## REST API

### Authentication

| Method | Endpoint | Description |
|---|---|---|
| POST | /api/auth/register | Register |
| POST | /api/auth/login | Login |

### Events

| Method | Endpoint | Description |
|---|---|---|
| GET | /api/events | Search and pagination |
| GET | /api/events/{id} | Event details |
| POST | /api/events | Create event |
| PUT | /api/events/{id} | Update event |
| DELETE | /api/events/{id} | Cancel event |
| PATCH | /api/events/{id}/status | Restore event |

### Event Seats

| Method | Endpoint | Description |
|---|---|---|
| GET | /api/events/{eventId}/seats | Get event seats |
| POST | /api/events/{eventId}/seats/reservations | Reserve seats |
| PATCH | /api/events/{eventId}/seats/reservations | Release seats |

### Bookings

| Method | Endpoint | Description |
|---|---|---|
| POST | /api/bookings | Create booking |
| GET | /api/bookings/{bookingId} | Get personal booking |
| GET | /api/bookings/my | Get personal booking history |
| DELETE | /api/bookings/{bookingId} | Cancel booking |

### Halls

| Method | Endpoint | Description |
|---|---|---|
| GET | /api/halls | Get halls |
| GET | /api/halls/{id} | Get hall details |
| POST | /api/halls | Create hall |
| PUT | /api/halls/{id} | Update hall |
| DELETE | /api/halls/{id} | Delete hall |

## HTTP Status Codes

| Code | Meaning |
|---|---|
| 200 | Successful request |
| 201 | Resource created |
| 204 | Successful operation without response body |
| 400 | Invalid request |
| 401 | Authentication required or invalid |
| 403 | Insufficient permissions |
| 404 | Resource not found |
| 409 | Conflict |
| 500 | Unexpected server error |

פעולות יצירת אירוע, אולם והזמנה מחזירות
201 Created עם Location Header.

## Error Handling and Logging

המערכת כוללת Middleware מרכזי לטיפול בחריגות
ולהחזרת תגובות ProblemDetails.

CorrelationIdMiddleware מקשר בין הבקשה לבין הלוגים
באמצעות מזהה בקשה ייחודי.

NLog משמש לכתיבת לוגים למסוף ולקובץ.

- Information: incoming requests.
- Warning: resource conflicts.
- Error: other caught exceptions.

קוד האפליקציה משתמש בממשק ILogger של .NET.

אין לשמור סיסמאות, JWT Tokens או פרטי התחברות בלוגים.

## Local Setup

### Prerequisites

- .NET 10 SDK
- PostgreSQL
- Git
- EF Core CLI

### 1. Clone repository

```powershell
git clone https://github.com/s0534126922-ai/EventTicketing.git
cd EventTicketing
```

### 2. Restore dependencies

```powershell
dotnet restore
```

### 3. Install EF Core CLI

אם הכלי עדיין אינו מותקן:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.15
```

### 4. Configure database and JWT

יש ליצור מסד נתונים מקומי בשם eventticketing.

יש להחליף את הערכים המסומנים בסוגריים משולשים
בערכים המתאימים לסביבה המקומית.

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=eventticketing;Username=postgres;Password=<LOCAL_DB_PASSWORD>" --project EventTicketing.API
dotnet user-secrets set "Jwt:Key" "<SECRET_KEY_AT_LEAST_32_BYTES>" --project EventTicketing.API
dotnet user-secrets set "Jwt:Issuer" "EventTicketing" --project EventTicketing.API
dotnet user-secrets set "Jwt:Audience" "EventTicketing.Client" --project EventTicketing.API
```

יש לבחור מפתח JWT אקראי וחזק.

אין לשמור פרטי התחברות אמיתיים ב-Git.

### 5. Configure demo accounts

הפרטים הבאים מיועדים להדגמה מקומית בלבד,
במסד נתונים חדש ונפרד מסביבת הייצור.

```powershell
dotnet user-secrets set "SeedAdmin:Name" "Demo Admin" --project EventTicketing.API
dotnet user-secrets set "SeedAdmin:Email" "admin@example.com" --project EventTicketing.API
dotnet user-secrets set "SeedAdmin:Password" "LocalAdmin2026!" --project EventTicketing.API
dotnet user-secrets set "SeedDemo:Enabled" "true" --project EventTicketing.API
dotnet user-secrets set "SeedDemo:Name" "Demo User" --project EventTicketing.API
dotnet user-secrets set "SeedDemo:Email" "user@example.com" --project EventTicketing.API
dotnet user-secrets set "SeedDemo:Password" "LocalUser2026!" --project EventTicketing.API
```

### Local Demo Credentials

| Role | Email | Password |
|---|---|---|
| Admin | admin@example.com | LocalAdmin2026! |
| User | user@example.com | LocalUser2026! |

פרטים אלו הם דוגמה להגדרת משתמשים מקומיים בלבד.
אין להשתמש בהם בסביבת ייצור.

### 6. Apply migrations

```powershell
dotnet ef database update --project EventTicketing.Data --startup-project EventTicketing.API
```

### 7. Run application

```powershell
dotnet run --project EventTicketing.API
```

Swagger:

http://localhost:5066/swagger/index.html

### Seed Data

בעת הפעלת המערכת עם הגדרות ההדגמה,
נוצרים הנתונים הבאים במסד נתונים חדש:

- חשבון מנהל.
- חשבון משתמש.
- שני אולמות.
- 80 מקומות ישיבה בשני האולמות.
- 12 אירועים עתידיים.
- מקומות ישיבה המשויכים לאירועים.

המערכת אינה יוצרת הזמנות מראש.

מנגנון ה-Seed נמנע מיצירה חוזרת של נתוני ההדגמה
כאשר האולמות המתאימים כבר קיימים.

האירועים נוצרים ביחס למועד ההפעלה הראשונה.
אירועים ישנים אינם מתחדשים אוטומטית.

## Automated Tests

פרויקט הבדיקות כולל:

- Unit Tests with Moq.
- PostgreSQL Integration Tests.
- REST API Integration Tests.
- Authentication and Authorization Tests.
- Optimistic Concurrency Tests.
- Serializable Transaction Tests.
- Commit and Rollback Tests.
- HTTP 409 Conflict Tests.

### Test Database

בדיקות האינטגרציה משתמשות במסד נתונים מקומי נפרד:

eventticketing_test

יש ליצור את מסד הנתונים ולהגדיר את החיבור
ב-User Secrets של פרויקט הבדיקות.

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=eventticketing_test;Username=postgres;Password=<LOCAL_DB_PASSWORD>" --project EventTicketing.Tests
```

יש להחיל את המיגרציות גם על מסד הבדיקות:

```powershell
dotnet ef database update --project EventTicketing.Data --startup-project EventTicketing.API --connection "Host=localhost;Port=5432;Database=eventticketing_test;Username=postgres;Password=<LOCAL_DB_PASSWORD>"
```

בדיקות האינטגרציה כוללות מנגנון אימות
המונע הרצה מול מסד נתונים שאינו מסד הבדיקות המקומי.

אין להריץ את בדיקות מסד הנתונים מול סביבת הייצור.

### Run Tests

```powershell
dotnet build
dotnet test --no-build
```

Latest verified test result:

59 passed, 0 failed, 0 skipped.

בדיקות האינטגרציה רצות בזו אחר זו כדי למנוע
התנגשויות בין בדיקות עצמאיות.

בדיקות התחרות הייעודיות משתמשות במספר DbContexts
וטרנזקציות עצמאיות כדי לאמת את מנגנוני ההגנה.

## Deployment Plan

- Backend: Render Web Service with Docker.
- Database: Neon PostgreSQL.
- Frontend: Render Static Site with HTML, CSS and JavaScript.

הפריסה בענן וחיבור צד הלקוח יושלמו בשלבים הבאים.