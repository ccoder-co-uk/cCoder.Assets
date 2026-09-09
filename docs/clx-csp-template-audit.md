# CLX upgrade template and workflow refresh

This refresh targets the B2B acceptance copy served at `https://localhost:7119`, with app 1 (`localhost`) and app 21 (`demo.dev.localhost`). It is for the CLX `upgrade/ccoder-core-latest` build, not the newer CLX standardisation branch.

## Sources and ownership

- `Data/demo.dev.localhost/Common Cache` contains the authoritative team component changes plus the reviewed CSP changes: 57 components and 37 non-framework scripts.
- `Data/CLX Upgrade/localhost/App` isolates 35 app-owned CSP component updates from the newer generic localhost snapshot.
- `Data/demo.dev.localhost/App` contains the corrected approval email and Generate Offers flow.
- `Static/CLX Upgrade/bootstrap/lib/widgets` records the five reviewed shared widget sources. They are maintained and built in **cCoder.Core**, not imported into Common Cache. Core PR: https://github.com/ccoder-co-uk/ccoder.Core/pull/268.
- `tools/clx-workflow-type-migrations.json` records the 22 exact activity-type substitutions verified against the supplied flow exports. The CLX baseline builder carries the same map and changes activity type metadata only. This avoids checking in configuration or the credentials present in the production-pull flow.

The generated `Packages/CLX Upgrade` overlay has separate Common Cache, first-app and Demo packages. It must be combined with the pinned compatible Core Assets baseline, as described in `clx-upgrade-ui-baseline.md`.

## CSP changes

104 stored components/scripts were updated through the domain APIs. The 276 directly convertible template expressions were checked against the deployment's Kendo 2024.2.514 template implementation: the functions produce matching output while dynamic `Function` construction is blocked. Encoded `#:` expressions retain HTML encoding; raw `#=` expressions retain their existing output semantics.

Runtime composition was also updated for grid commands, picker items, detail fields/titles, folder trees, file-version rows, chart labels/tooltips, form details and the business-process editor. Default query-builder/grid-builder columns now use functions, including the JavaScript emitted by the grid builder. Grid command strings retain their legacy Kendo compilation behavior for other consumers; migrated functions bypass that path.

The scan covered 697 component/script/template/layout/page records, plus expanded page contents. No expression-template markers were found in the expanded page contents or layout HTML. Vendor libraries were not rewritten.

Remaining textual matches are not all active Kendo compilation:

- The four Summary leaderboard components already render with functions; their old HTML template examples are unused.
- DocumentManagement replaces its folder label marker explicitly; it does not pass that expression string to Kendo.
- Picker examples and builder template-source fields contain template syntax as text. Built-in builder defaults are now recognized and rendered as functions.
- Custom author-entered templates can contain arbitrary JavaScript and still require individual conversion. This pass does not introduce a runtime JavaScript interpreter or relax CSP.
- OfferFundingDetails has a legacy `eval(NewFormula)` template in its metadata. The deployed read-only detail view ignores metadata templates, so this is not an active Kendo compilation path. Reworking financial formula evaluation is not part of this template migration.

## Workflows

All six Demo flow exports differed from the local definitions only in activity type metadata. The five remaining namespace-only updates were applied with existing configuration intact. Deleted acceptance/test flows from the main-app export were not recreated.

Generate Offers no longer performs the obsolete `Core/App` request. The existing sequencing step becomes `Use App Context`, and its outgoing configuration expression reads `variables["App"]`, which Start has already populated. No direct Start-to-retirement link was added. No business workflows were executed during verification.

## Validation and release dependency

- Core frontend regression suite and analyzer-enabled Core/CLX Web builds pass.
- Stored API payloads were re-read and matched after import.
- The served framework bundle hash matches the rebuilt Core bundle.
- Authenticated workflow and bucket-administration UI checks use the fixed Core build.
- Packer tests, CSP/bucket tests, generated package payload comparisons and manifest checks cover the renewed baseline.

The Demo dashboard renders its state chart and funding-analysis rows. Its unchanged shared `Charts/FinancialForecast` component still raises a missing `Value` error in `buildSeriesDataset` for the current data. This is separate from Kendo template compilation and has not been changed by this pass.

**The team baseline must be paired with a Core release containing PR 268.** The existing published CLX dependency predates the shared widget function support. Local review uses the fixed Core source; producing a backup ZIP alone would not update the team's application binaries.
