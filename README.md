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
Data/Common Cache/Resources/{resource key}/{culture}.json
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
Data/{source domain}/Components/{name}.json
Data/{source domain}/Resources/{resource key}/{culture}.json
Data/{source domain}/Scripts/{name}.json
```

Use `-appId {id}` when the hostname does not uniquely identify an app.
An app unpack includes every business-object type returned by the package export
API. Resources sharing a resource key and culture are kept together as a JSON
array in that key and culture's file.
