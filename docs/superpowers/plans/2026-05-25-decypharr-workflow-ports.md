# Lidarr Decypharr Workflow Ports Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port the two Decypharr-related command/tracking behaviors from the custom Sonarr/Radarr forks into Lidarr with regression coverage.

**Architecture:** Keep the patch inside existing Lidarr ownership boundaries. The command API decides the priority of API-submitted refresh commands, while `DownloadEventHub` completes lifecycle cleanup by dropping tracking only after successful download-client removal.

**Tech Stack:** C# / .NET 8, ASP.NET Core controller tests, NUnit, Moq, FluentAssertions

---

### Task 1: API Refresh Command Priority

**Files:**
- Modify: `src/NzbDrone.Api.Test/Lidarr.Api.Test.csproj`
- Create: `src/NzbDrone.Api.Test/Commands/CommandControllerFixture.cs`
- Modify: `src/Lidarr.Api.V1/Commands/CommandController.cs`

- [ ] **Step 1: Write the failing API unit test**

Add a `Lidarr.Api.V1` project reference to `Lidarr.Api.Test.csproj`, then create a test fixture which constructs `CommandController` with `KnownTypes` containing `RefreshMonitoredDownloadsCommand`, supplies a seekable JSON request body and verifies:

```csharp
Mocker.GetMock<IManageCommandQueue>()
    .Verify(v => v.Push(It.IsAny<RefreshMonitoredDownloadsCommand>(),
        CommandPriority.High,
        CommandTrigger.Manual), Times.Once());
```

- [ ] **Step 2: Run the API test to verify it fails**

Run:

```powershell
dotnet test src/NzbDrone.Api.Test/Lidarr.Api.Test.csproj --filter FullyQualifiedName~CommandControllerFixture
```

Expected: FAIL because API-posted `RefreshMonitoredDownloadsCommand` is currently queued at `CommandPriority.Normal`.

- [ ] **Step 3: Implement the minimal command priority port**

Update `CommandController.cs` to import `NzbDrone.Core.Download` and assign high priority for either supported high-priority command:

```csharp
var priority = commandType == typeof(ManualImportCommand) ||
               commandType == typeof(RefreshMonitoredDownloadsCommand)
    ? CommandPriority.High
    : CommandPriority.Normal;
```

- [ ] **Step 4: Re-run the focused API test**

Run:

```powershell
dotnet test src/NzbDrone.Api.Test/Lidarr.Api.Test.csproj --filter FullyQualifiedName~CommandControllerFixture
```

Expected: PASS.

### Task 2: Stop Tracking Removed Imports

**Files:**
- Create: `src/NzbDrone.Core.Test/Download/DownloadEventHubFixture.cs`
- Modify: `src/NzbDrone.Core/Download/DownloadEventHub.cs`

- [ ] **Step 1: Write the failing core unit test**

Create `DownloadEventHubFixture : CoreTest<DownloadEventHub>`, set up a removable tracked item and download client with `RemoveCompletedDownloads = true`, send `DownloadCanBeRemovedEvent`, then verify:

```csharp
Mocker.GetMock<ITrackedDownloadService>()
    .Verify(v => v.StopTracking(_trackedDownload.DownloadItem.DownloadId), Times.Once());
```

- [ ] **Step 2: Run the core test to verify it fails**

Run:

```powershell
dotnet test src/NzbDrone.Core.Test/Lidarr.Core.Test.csproj --filter FullyQualifiedName~DownloadEventHubFixture
```

Expected: FAIL because `DownloadEventHub` currently removes the item without calling `StopTracking()`.

- [ ] **Step 3: Implement the minimal tracking cleanup port**

Inject `ITrackedDownloadService` into `DownloadEventHub` and call it only after removal succeeds:

```csharp
downloadClient.RemoveItem(trackedDownload.DownloadItem, true);
trackedDownload.DownloadItem.Removed = true;
_trackedDownloadService.StopTracking(trackedDownload.DownloadItem.DownloadId);
```

- [ ] **Step 4: Re-run the focused core test**

Run:

```powershell
dotnet test src/NzbDrone.Core.Test/Lidarr.Core.Test.csproj --filter FullyQualifiedName~DownloadEventHubFixture
```

Expected: PASS.

### Task 3: Verification and Publication

**Files:**
- Review: all changed files above and `docs/superpowers/specs/2026-05-25-decypharr-workflow-ports-design.md`

- [ ] **Step 1: Run the touched project test suites**

Run:

```powershell
dotnet test src/NzbDrone.Api.Test/Lidarr.Api.Test.csproj
dotnet test src/NzbDrone.Core.Test/Lidarr.Core.Test.csproj
dotnet build src/Lidarr.sln
```

Expected: all commands exit successfully with no test failures.

- [ ] **Step 2: Inspect the patch scope**

Run:

```powershell
git status --short --branch
git diff --stat HEAD
git diff HEAD -- src/Lidarr.Api.V1/Commands/CommandController.cs src/NzbDrone.Core/Download/DownloadEventHub.cs src/NzbDrone.Api.Test src/NzbDrone.Core.Test
```

Expected: only the design/plan documents, two production files, one test project reference, and two focused fixture files are present.

- [ ] **Step 3: Commit and publish**

Stage only the intended Lidarr files, create a concise commit, push the branch to the GitHub fork and Gitea mirror, and record remote branch URLs in Obsidian. Do not alter the Tubifarry worktree in this task.
