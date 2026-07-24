# cCoder.Assets

Source-controlled cCoder components, resources, scripts, and generated
installation packages.

## Running the packer

Run commands from the repository root:

```powershell
dotnet run --project src/cCoder.Packer -- {command}
```

The default paths come from `appsettings.json` beside the executable:

```json
{
  "Packer": {
    "DataPath": "Data",
    "PackagesPath": "Packages"
  }
}
```

Use `-dataPath {path}` or `-packagesPath {path}` to override either path for one
command.

## Commands

### Create an application from a baseline

```powershell
dotnet run --project src/cCoder.Packer -- create `
  -api https://ccoder.co.uk `
  -name test `
  -tenant default `
  -user {user} `
  -pass {password} `
  -baseline "C:\Data\Github\cCoder\cCoder.Assets\Packages\First Time Setup"
```

This authenticates against the supplied API, creates an application named
`test` for the supplied tenant on `test.ccoder.co.uk`, and then:

- imports every package below `App` into the newly created application; and
- imports every package below `Common Cache` into the deployment-wide common
  cache.

The platform's existing application-creation behavior makes the authenticated
user the initial application administrator. The command currently consumes the
existing app, package-import, and common-object endpoints as-is; those calls may
need to move when the ongoing API standardisation work lands.

### Unpack the common cache

```powershell
dotnet run --project src/cCoder.Packer -- `
  -unpack commoncache `
  -from https://ccoder.co.uk/
```

This authenticates against the source application, exports every supported
common-cache business-object type, and splits the export into reviewable files:

```text
Data/Common Cache/{resource key}/Components/{name}.json
Data/Common Cache/{resource key}/Resources/{culture}.json
Data/Common Cache/{resource key}/Scripts/{name}.json
```

### Unpack an application

```powershell
dotnet run --project src/cCoder.Packer -- `
  -unpack app `
  -from https://ccoder.co.uk/
```

This exports every business-object type in the application package and groups
the files by source domain, resource key, and type:

```text
Data/{source domain}/{resource key}/Components/{name}.json
Data/{source domain}/{resource key}/Resources/{culture}.json
Data/{source domain}/{resource key}/Scripts/{name}.json
Data/{source domain}/{resource key}/{other type}/{name}.json
```

Use `-appId {id}` when the source hostname does not uniquely identify an
application. Credentials can be supplied with `-user` and `-password`; when
omitted, the packer uses `CCODER_USER` and `CCODER_PASSWORD` or prompts at the
console.

Resources sharing a key and culture are stored together as a JSON array.
Types without a resource key are placed beneath `Default`.

Every split business object contains two packaging fields:

- `PackageType` preserves the API type needed when rebuilding packages.
- `IncludeInSubSequentImports` controls the package destination. New exports
  default this value to `false`; set it to `true` for baseline data required
  during first-time setup.

### Build packages

```powershell
dotnet run --project src/cCoder.Packer -- -pack
```

This rebuilds the complete `Packages` directory from every JSON file under
`Data`. Existing generated package output is replaced. Objects are grouped into
one package per resource key and API type.

All common-cache and application objects are written to source-owned package
trees:

```text
Packages/Common Cache/{resource key}/{API type}.json
Packages/{source domain}/{resource key}/{API type}.json
```

Objects whose `IncludeInSubSequentImports` value is `true` are also copied into
the curated first-time-setup tree:

```text
Packages/First Time Setup/Common Cache/{resource key}/{API type}.json
Packages/First Time Setup/App/{resource key}/{API type}.json
```

The application homepage is baseline data: it belongs in the source-owned
application package and is additionally included in the first-time-setup page
package.

The packaging-only fields are removed from the business-object JSON embedded in
each generated package.

`Packages/manifest.json` is rebuilt at the same time. It is the stable entry
point for downstream setup and integration tests and records:

- the relative path of every package;
- whether it belongs to first-time setup;
- its application or common-cache source;
- its resource-key category and API item types; and
- a SHA-256 checksum for integrity and deterministic-consumption checks.

The generated package envelope matches the `cCoder.Packaging` API model:
`Name`, `Description`, `Category`, `SourceApi`, and an `Items` collection whose
members contain `Type` and JSON `Data`. Item types use the current
`{domain}/{entity}` API identifiers, such as
`ContentManagement/Component` and `Workflow/FlowDefinition`.

### Generate the asset-usage report

```powershell
dotnet run --project src/cCoder.Packer -- -report
```

This scans every source directory under `Data`, follows layout, page, component,
resource, script, and dynamic `loadComponent()` references, and writes:

```text
reports/asset-usage-report.md
```

The report resolves application assets within their own source first and then
falls back to the common cache. This prevents an identically named component in
another exported application from creating a false dependency.

## Normalising an existing export

The checked-in baseline has been normalised so resource keys follow the API
domains consumed by components. Related scripts, resources, pages, and layouts
inherit an unambiguous domain key; shared, presentation-only, or ambiguous
assets use `Default`.

To review the same transformation against a future export without changing the
source tree:

```powershell
./tools/normalise-asset-keys.ps1 `
  -DataPath ./Data `
  -OutputPath ./normalised-data
```

The normaliser also repairs the legacy `source/type/key` folder order, writes
the canonical `source/key/type` structure, and preserves explicitly configured
`IncludeInSubSequentImports` values. When the field is absent on a legacy
snapshot, it defaults to `true` because this repository is the source of the
first-time-setup baseline. Run the report and package commands against the
separate output before promoting it.
