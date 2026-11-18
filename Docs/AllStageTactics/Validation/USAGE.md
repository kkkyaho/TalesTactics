# All-stage execution tools

These tools prepare and check local evidence. Their presence does not mean Unity tests, builds or the campaign have passed. Local music and build outputs must remain local.

## Order

1. Complete the actual Unity checks and record the exact tested source manifest. Restore the new task baseline's protected generated assets if tests changed them; when restoring a loaded asset, synchronously reimport it and confirm it is clean before capturing build inputs.
2. Capture `Before` separately for Normal and Review immediately before each build. Each call creates a new GUID directory under `Logs/AllStageTactics20261118` and prints its manifest path. Existing outputs are rejected.
3. Run the external C# helper through Unity Pipeline `run_script`: source `Docs/AllStageTactics/Validation/Build-AllStageTactics.cs`, class `AllStageTacticsBuild`, method `Run`, one argument set to the exact desired output below. A `Scheduled` receipt only means the callback was queued. Wait for its referenced `finalResult` and require the actual successful BuildReport.
4. Capture `After` with that build's `Before` manifest and actual BuildReport. Require `passed=true`, unchanged inputs and matching output hashes. Repeat sequentially for the other build; never retry while a build is active or reuse a previous output.
5. Run `Validate-AllStageCampaignWindows.ps1 -Mode Review`. It inspects the normal build without starting it, then starts only the REVIEW executable in a new isolated GUID directory. Its own exit0 and final `validation.json` must pass. `Inspect` only reads metadata and is not a campaign pass.
6. After execution, compare protected files and the complete user tree to the **new** task baseline and restore Editor state. Do not hide StorageTests/TestResults changes or count root Unity logs as saved user data.

Build outputs:

- Normal: `C:/dev/TalesTactics/Builds/AllStageTacticsWindows20261118`
- Review: `C:/dev/TalesTactics/Builds/AllStageTacticsReview20261118`

Example source capture (replace receipt placeholders with this invocation's actual paths):

```powershell
pwsh -NoProfile -File C:/dev/TalesTactics/Docs/AllStageTactics/Validation/Capture-AllStageBuildSource.ps1 -Phase Before -Kind Normal -SourceLabel all-stage-final -TestSourceManifestPath <absolute-tested-source-manifest>
pwsh -NoProfile -File C:/dev/TalesTactics/Docs/AllStageTactics/Validation/Capture-AllStageBuildSource.ps1 -Phase After -Kind Normal -SourceLabel all-stage-final -BeforeManifestPath <normal-before-manifest> -BuildResultPath <normal-actual-build-result> -TestSourceManifestPath <absolute-tested-source-manifest>
pwsh -NoProfile -File C:/dev/TalesTactics/Docs/AllStageTactics/Validation/Validate-AllStageCampaignWindows.ps1 -Mode Review -SourceLabel all-stage-final -SourceManifestPath <review-after-manifest> -TimeoutSeconds 3600
```

Use PowerShell7 with standard native argument passing for JSON-bearing Unity CLI commands. PowerShell5 native quoting previously stripped JSON quotes before dispatch. Do not overwrite dispatch receipts: use new GUID paths for every attempt, including rejected attempts.

## Required coverage

The new runtime flag `--campaign-all-stages-profile` accompanies the existing GUID bootstrap `--campaign-flow-review <guid>`. Old profiles retain their original scope.

- Exact stages6..55: main20, personal dialogue10, personal combat20.
- All40 battles use production commands, normal enemy turns, victories and reward commits. Trial requirements use actual healing/interruption/drain/HP-cost/guarded-damage observations; no health, gauge, level, flag or outcome fixture injection.
- All50 stages render before/after stories, persist ending cursors, prove duplicate reward protection and reload through the title.
- Exact180 nonuniform1280x720 PNGs: both story openings for all50 plus start/result for all40 battles. Exact100 before/after title-reload JSON files have matching complete state hashes, excluding only SavedAt.
- Main economic checks reuse the frozen RewardVariety contract for the first20 rows. Personal checks independently reconcile all30 stages' purchases, gold0/150, EXP10 plus actionEXP, inventory and ten exact signature weapons. Dialogue EXP goes only to its owner.
- Normal build excludes REVIEW/test/editor code; REVIEW requires the storage bootstrap, all-stage flag and background execution. PID/start time/executable are checked before cleanup; other processes are preserved.
- Complete recursive user files and empty directories are checked, excluding only root `Player.log` and `Player-prev.log`. Reparse points or inventory/hash races fail. No user save is restored by the wrapper.

## Evidence limits

The planner is automated, accelerated and deterministic. It chooses one alternating route per personal arc; passing does not establish every route permutation, human balance/pacing, visual quality or mid-dialogue process-restart recovery. PNG pixel checks are not human visual review.

Before/After source captures include all Assets, Packages and ProjectSettings files, including untracked/ignored/local music. They link stable input hashes to the actual BuildReport and EXE/DLL/output hashes. They do not hash imported Library artifacts or prove a hermetic build. `TestSourceManifestPath` identifies the supplied test receipt by hash; the caller must independently check that the receipt's tested inputs equal the final build inputs.

Fresh task baseline: `Logs/AllStageTactics20261118/Baseline-d8380e47deed41c19c7b19acfa08ce42/baseline.json`. It captured current12 protected exact-byte copies and current5272 user files/2552 directories at base `7a2cdccec4b7a5d98508421bfd9d58b0f294f24c` on `develop`. Prior CommercialSlice evidence is historical, not this task's baseline or verdict.