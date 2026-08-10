# cCoder.Assets browser acceptance tests

This project tests source-controlled UI assets against externally published
cCoder.Core applications. It must not reference Core application projects.

The harness expects:

- `CCODER_CORE_PUBLISH_ROOT`: a directory containing the published Web,
  HostedServices, and Workflow applications;
- `AppSecurity__ConnectionString`: a SQL Server connection string used only as
  the base for a uniquely suffixed Core acceptance database;
- `Security__ConnectionString`: the equivalent SSO database connection string;
- `Security__DecryptionKey`: the acceptance encryption key.

The fixture owns the complete lifecycle: create temporary databases, launch the
published processes, submit first-time setup using this checkout's packages,
run Playwright contracts, stop processes, and drop databases even after a test
failure.

Every failed component contract writes a diagnostic folder containing:

- `page.png` — full-page screenshot;
- `page.html` — final DOM;
- `browser.log` — console errors, page errors, failed requests, and HTTP 4xx/5xx
  responses;
- `applications.log` — Web, HostedServices, and Workflow output.

`Contracts/components.json` is the inventory. A component is not considered
covered until it declares a render route or generated host page, a stable ready
selector, authentication requirements, required API responses, and its primary
interaction assertions.
