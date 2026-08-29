# Stocktake Delivery Architecture Progress

Last updated: 27 August 2026

## Status

Planning, implementation, and verification are complete for tickets #6 through #9.

## Completed

- Reviewed the repository for deepening opportunities, prioritizing recent Stocktake Delivery and Stocktake Export hotspots.
- Produced a visual architecture report with four candidates. The top recommendation was to collapse the stocktake delivery workflow into one deep module.
- Completed and confirmed a grilling session covering the workflow scope, seam placement, typed outcomes, cancellation, configuration snapshots, concurrency, adapter structure, file lifecycle, migration, and testing strategy.
- Added the agreed domain language to `CONTEXT.md`:
  - Stocktake Export
  - Stocktake Delivery
  - Stocktake Delivery Workflow
- Published the approved implementation specification as GitHub issue [#5 — Deepen the Stocktake Delivery Workflow](https://github.com/jedipi/Quick-Stocktaker/issues/5).
- Published four `ready-for-agent` tracer-bullet tickets:
  - [#6 — Create Stocktake Exports through the delivery workflow](https://github.com/jedipi/Quick-Stocktaker/issues/6)
  - [#7 — Deliver Stocktake Exports through FTP and SFTP adapters](https://github.com/jedipi/Quick-Stocktaker/issues/7)
  - [#8 — Deliver Stocktake Exports by email](https://github.com/jedipi/Quick-Stocktaker/issues/8)
  - [#9 — Contract the legacy delivery path and verify the cutover](https://github.com/jedipi/Quick-Stocktaker/issues/9)

## Ticket #6 Implementation

- Added `IStocktakeDeliveryWorkflow` as the highest behavioral and testing seam for local Stocktake Export creation.
- Added immutable Stocktake Export and typed delivery result models.
- Added result-producing CSV export while retaining a temporary compatibility bridge for the email and FTP/SFTP tickets.
- Added typed outcomes for success, no stocktake data, invalid configuration, cancellation, an operation already in progress, and failure.
- Captured site and device identity before the asynchronous stocktake item read so in-flight preference changes apply only to the next export.
- Added a shared application-wide operation gate that rejects overlapping operations without queueing.
- Registered the operation gate as an Autofac singleton while keeping the workflow transient, avoiding capture of the lifetime-scoped repository.
- Migrated the local CSV save/share command to the workflow while preserving the existing alert and action-sheet behavior.
- Kept email and FTP/SFTP on the legacy path for tickets #8 and #7.
- Added workflow tests for success, no data, cancellation, unexpected failure and single logging, coherent identity snapshots, and cross-instance concurrency.
- Added thin ViewModel tests for no-data rendering and the existing Share/Save actions.

## Code Review Fixes

- Restored portable Android SDK commands in `AGENTS.md`; the actual machine path remains local to verification commands.
- Removed the local save/share action's dependency on the shared legacy `_exportedFile` field. Each action sheet now closes over its own immutable workflow result.
- Replaced synchronous CSV serialization with CsvHelper's cancellable asynchronous writer.
- CSV output is written to a unique sibling temporary file and promoted to the final path only after successful completion; cancellation and failures remove the temporary file.
- Added a regression test proving an earlier action sheet cannot save a later export.
- Added a complete invariant-culture CSV compatibility test covering header order, delimiter, values, and line shape through the workflow seam.

## Ticket #7 Implementation

- Added an explicit configured FTP/SFTP delivery operation to the Stocktake Delivery Workflow.
- Captured and validated the complete remote destination configuration once per operation using FluentValidation.
- Added separate internal FTP and SFTP production adapters behind one transfer seam.
- Preserved remote paths, directory creation, overwrite behavior, generated-file retention, and existing user-visible delivery results.
- Propagated cancellation through transfer, including SSH.NET disposal-related cancellation exceptions, without unexpected-failure logging.
- Mapped adapter faults to one workflow error log and safe UI information without exposing credentials or raw connection details.
- Migrated the FTP/SFTP ViewModel command to typed workflow results while preserving progress, cancellation, alert, icon, and disposal sequencing.
- Added focused coverage for configuration validation and snapshots, protocol selection, adapters, cancellation, faults, concurrency, and ViewModel presentation.

## Verification

- Full .NET 10 unit-test suite: passed, 87/87 tests.
- Ticket-scoped formatting verification: passed.
- Repository-wide formatting verification: passed with no changes required.
- Android MAUI Debug build for `net10.0-android`: passed with 0 errors.
- Existing warnings remain for known package advisories affecting `SQLitePCLRaw.lib.e_sqlite3` and `SSH.NET`, plus existing `FontAwesome.Equals` member-hiding warnings in the Android build.

## Approved Direction

- Expose explicit operations for local export creation, email delivery, and configured FTP/SFTP delivery.
- Replace mutable Stocktake Export and delivery state with typed results.
- Capture stocktake data, identifying metadata, and destination configuration once per operation.
- Keep prompts, progress presentation, alerts, action sheets, and platform share/save behavior in the ViewModel.
- Keep email transport behind the workflow and place FTP/SFTP adapters behind an internal transfer seam.
- Preserve current user-visible behavior and generated-file retention.
- Do not add retries, JSON export, cloud destinations, navigation-driven cancellation, or file cleanup in this change.
- Remove the shallow factory, mutable exporter state, mutable email configuration, redundant FTP validation, and duplicate failure logging only after all delivery paths have migrated.

## Next Step

Run the final ticket #9 acceptance audit and close it when all criteria are confirmed.

## Existing Unrelated Worktree State

Agent and domain guidance was committed separately in `edce26f`. The generated landing-page archive remains ignored and is not part of the delivery change.
