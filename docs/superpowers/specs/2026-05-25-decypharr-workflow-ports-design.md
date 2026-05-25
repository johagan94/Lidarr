# Lidarr Decypharr Workflow Ports Design

## Context

The Unraid media stack uses Decypharr-backed download handling and custom
`realzombee` Radarr/Sonarr images. Lidarr currently runs the standard
LinuxServer nightly image and is missing two workflow changes already used in
the sibling Arr forks.

Tubifarry is an independently deployed Lidarr plugin with separate Soulseek
cleanup and retry issues. Those plugin issues are explicitly outside this
Lidarr change set.

## Scope

Port only these two Lidarr core behaviors from the corresponding Radarr/Sonarr
fork patterns:

1. Commands submitted through the API as
   `RefreshMonitoredDownloadsCommand` are queued with high priority, matching
   `ManualImportCommand`.
2. When an imported/removed tracked download is removed from its download
   client, Lidarr immediately stops tracking that download ID.

Do not port Radarr video probing behavior, add Tubifarry code, or change
Unraid mount/performance configuration in this patch.

## Implementation

### Command Priority

Update `src/Lidarr.Api.V1/Commands/CommandController.cs` to import the
download command namespace and assign `CommandPriority.High` when the
submitted command type is either `ManualImportCommand` or
`RefreshMonitoredDownloadsCommand`.

This change affects API-issued refresh commands only. Existing scheduler
behavior remains unchanged.

### Download Tracking Cleanup

Update `src/NzbDrone.Core/Download/DownloadEventHub.cs` to inject
`ITrackedDownloadService`. After a successfully imported tracked download is
removed from its download client and marked removed, call `StopTracking()` for
that download ID.

The cleanup is placed after successful download-client removal so a failed
removal does not discard track state prematurely.

## Testing

Add or adapt focused tests in Lidarr's existing test assemblies:

- Verify API command submission assigns high priority to
  `RefreshMonitoredDownloadsCommand` while ordinary command types remain
  normal priority.
- Verify successful removal of an imported download invokes
  `ITrackedDownloadService.StopTracking()` with its download ID.
- Keep existing build/test coverage passing for touched projects.

If a touched class lacks an isolated fixture, add the smallest fixture using
the existing NUnit/AutoMoq patterns rather than broad integration setup.

## Publishing and Deployment

After tests and build verification:

1. Publish the Lidarr fork branch to the user's GitHub fork and Gitea mirror.
2. Build or configure the container image path for the patched Lidarr build.
3. Update Unraid only when the patched image is available, retaining the
   existing `/data` `rslave` bind propagation and CPU-share controls.

Tubifarry plugin changes will be handled in a separate branch and deployment.

## Rollback

Rollback consists of switching the Lidarr container image/template back to
the prior LinuxServer nightly image. No database migration or path-layout
change is introduced by this patch.
