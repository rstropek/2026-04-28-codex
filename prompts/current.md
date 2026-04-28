# Initial Application Creation

* Create app skeleton
* For each project, create one dummy test (true == true) to ensure the test project is set up correctly
* DB
  * One placeholder entity ("Questionaires"), with surrogate ID, string code (max. 50 chars), string description (max. 200 chars)
  * Let us control folder + name of SQLite database file via appsettings.json (use absolute path to CWD + "Questionaires.db" as default value)
* Web API: Ony "ping" endpoint that returns "pong"
* Frontend for now:
  * Single page
  * No responsive design, fix 1280px width, centered horizontally
  * Top-level menu with one placeholder item ("Questionnaires") that doesn't do anything when clicked
  * Do web api call to "ping" endpoint and display the result on the page to verify frontend-backend communication is working
* Create web API using `dotnet new webapi`
* Create unit test project using `dotnet new xunit`
* Create Vite frontend using `npm create vite@latest` with vanilla TypeScript template
* Create DB migrations + update database to create the initial schema with the "Questionnaires" table
