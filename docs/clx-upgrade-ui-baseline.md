# CLX upgrade UI baseline — 9 September 2026

`Data/demo.dev.localhost` contains the reviewed changes from the team's
`common-cache-localhost-2026-09-09.json` export of the supplied CLX acceptance
database baseline. It is a targeted overlay, not a complete app export.

The export is authoritative for the 32 changed shared components. One verified
compatibility correction is layered onto BucketedCompanyManagement: Portal Admin
has no SourceSystem setting, so the shared child handles that global context
without throwing on split(), while preserving app source-system filtering. The
two scenarios are covered by `tools/test-clx-bucket-company-scope.cjs` (Node.js is
required for this check). Other Content and Script remain unchanged, including
existing markup issues that require separate verification. The test Resource with Key and Name both a backtick is
excluded. No references to that resource were found in the export.

`BucketedCompanyManagement` and `BucketedRoleManagement` are shared children used
by app-level administration and Portal Admin. They belong in Common Cache.
Remove older app-owned copies when upgrading an existing database; otherwise
those copies mask the shared versions. Retain the separate administration hosts
and their existing access restrictions.

The Demo app overlay also contains `AccessRequestApprovedEmail`, updated to greet
the AccessRequest UserName and link to the application. It does not require an
invitation token or ask an existing account to set a password.

Run `./Rebuild-Packages.ps1` to build and test the sources, regenerate the report,
and publish these generated artifacts into the repository:

- `Packages/CLX Upgrade/common-cache-ui.json` — 32 shared components.
- `Packages/CLX Upgrade/demo-app-ui.json` — one Demo email template.
- `Packages/manifest.json` — checksums and item types for both packages.

These assets target the tested CLX upgrade branch. They are deliberately separate
from `Data/Default App`, whose newer standardisation baseline must not be replaced
with older upgrade assets.

In CLX.Applications, pass this directory as `-UiOverlayRoot` to
`tools/DevBaselineDeployment/New-ClxDevDeploymentBundle.ps1` when using the pinned
Core baseline. The generator applies the overlay after its legacy transformations
and its cleanup plan removes the obsolete app-owned child copies. A compatible
AssetsRoot containing `Packages/CLX Upgrade` is detected automatically.
