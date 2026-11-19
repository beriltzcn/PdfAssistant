# PdfAssistant Client

React and TypeScript frontend for the PdfAssistant API.

## Development

Start the API from the repository root:

```powershell
dotnet run --project PdfAssistant.Api
```

Start the client:

```powershell
cd PdfAssistant.Client
npm install
npm run dev
```

The client runs at `http://localhost:5173` and expects the API at
`http://localhost:5053`.

Set `VITE_API_BASE_URL` to use a different API address.

## Checks

```powershell
npm run lint
npm run build
```
