# AskAMuslimBackend

The project was generated using the [Clean.Architecture.Solution.Template](https://github.com/jasontaylordev/CleanArchitecture) version 9.0.12.

## Build

Run `dotnet build -tl` to build the solution.

## Run

To run the web application:

```bash
cd .\src\Web\
dotnet watch run
```

Navigate to https://localhost:5001. The application will automatically reload if you change any of the source files.

## Firebase push notifications

Push notifications need a Firebase service-account key (Firebase console → Project settings → Service accounts → Generate new private key). The key is a secret: it is git-ignored and must never be committed or published.

The app reads the credential at startup, in this order:

1. `Firebase:CredentialJson`: the full service-account JSON, raw or base64-encoded. In production set the `Firebase__CredentialJson` environment variable (or the `Firebase--CredentialJson` secret in Key Vault).
2. `Firebase:CredentialPath`: a path to the JSON file. A relative path is resolved against the app's output directory, then the content root. The default is `firebase-adminsdk.json`, so for local development you can drop the key into `src/Web/firebase-adminsdk.json`. The file is never copied into `dotnet publish` output.

Startup logs `Firebase initialized from ...` on success. If neither source works it logs one warning, `Firebase is not initialized ...`, and every push returns a failed result instead of being sent.

Example for a systemd service on the server:

```bash
base64 -w0 firebase-adminsdk.json   # paste the output as the value below
# /etc/systemd/system/<service>.service.d/firebase.conf
# [Service]
# Environment=Firebase__CredentialJson=<base64 output>
```

> **Rotate the old key.** A service-account key used to be committed to this repository (`src/Web/firebase-adminsdk.json`), so it is still in git history. Treat it as leaked: generate a new key in the Firebase console, deploy it with `Firebase__CredentialJson`, then delete the old key under Google Cloud console → IAM → Service accounts → Keys.

## Code Styles & Formatting

The template includes [EditorConfig](https://editorconfig.org/) support to help maintain consistent coding styles for multiple developers working on the same project across various editors and IDEs. The **.editorconfig** file defines the coding styles applicable to this solution.

## Code Scaffolding

The template includes support to scaffold new commands and queries.

Start in the `.\src\Application\` folder.

Create a new command:

```
dotnet new ca-usecase --name CreateTodoList --feature-name TodoLists --usecase-type command --return-type int
```

Create a new query:

```
dotnet new ca-usecase -n GetTodos -fn TodoLists -ut query -rt TodosVm
```

If you encounter the error *"No templates or subcommands found matching: 'ca-usecase'."*, install the template and try again:

```bash
dotnet new install Clean.Architecture.Solution.Template::9.0.12
```

## Test

The solution contains unit, integration, and functional tests.

To run the tests:
```bash
dotnet test
```

## Help
To learn more about the template go to the [project website](https://github.com/jasontaylordev/CleanArchitecture). Here you can find additional guidance, request new features, report a bug, and discuss the template with other users.