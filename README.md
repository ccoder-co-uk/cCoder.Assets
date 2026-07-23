# cCoder.Assets

Source-controlled cCoder components, resources, scripts, and generated packages.

## Unpacking assets

Run the packer from the repository root. It prompts for credentials unless
`CCODER_USER` and `CCODER_PASSWORD` are set.

```powershell
dotnet run --project src/cCoder.Packer -- `
  -unpack commoncache `
  -from https://ccoder.co.uk/
```

Common-cache objects are written beneath:

```text
Data/Common Cache/Components/{name}.json
Data/Common Cache/Resources/{name}.json
Data/Common Cache/Scripts/{name}.json
```

To unpack the app associated with the source hostname:

```powershell
dotnet run --project src/cCoder.Packer -- `
  -unpack app `
  -from https://ccoder.co.uk/
```

App objects are grouped by their owning domain:

```text
Data/{domain}/Components/{name}.json
Data/{domain}/Resources/{name}.json
Data/{domain}/Scripts/{name}.json
```

Use `-appId {id}` when the hostname does not uniquely identify an app.
Translations or other objects sharing the same business name are kept together
as a JSON array in that name's file.
