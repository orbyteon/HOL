import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";

const workflowPath = ".github/workflows/playmode-tests.yml";
const workflow = fs.readFileSync(workflowPath, "utf8");
const missionFlowPath =
  "Assets/Tests/PlayMode/DailyChallengeMissionFlowPlayModeTests.cs";
const missionFlow = fs.readFileSync(missionFlowPath, "utf8");

test("PlayMode workflow isolates its utility checkout from the Unity workspace", () => {
  assert.match(
    workflow,
    /Checkout actions for local composite steps[\s\S]*?path:\s*workflow-actions/,
    "the sparse .github/actions checkout must live outside the Unity project root"
  );
  assert.match(
    workflow,
    /uses:\s*\.\/workflow-actions\/\.github\/actions\/require-ci-green/,
    "the CI gate must execute from the isolated utility checkout"
  );
  assert.match(
    workflow,
    /uses:\s*\.\/workflow-actions\/\.github\/actions\/resolve-pr-merge-ref/,
    "merge-ref resolution must execute from the isolated utility checkout"
  );
});

test("an explicit preview label can validate any PR without taxing ordinary PRs", () => {
  assert.match(workflow, /pull_request:\s*\n\s*types:\s*\[labeled\]/);
  const pullRequestTrigger = workflow.match(
    /pull_request:\s*\n([\s\S]*?)\n\s*workflow_dispatch:/
  )?.[1] ?? "";
  assert.doesNotMatch(
    pullRequestTrigger,
    /\bpaths:/,
    "the explicit preview label is already the cost gate and must not be silently path-filtered"
  );
  assert.match(workflow, /github\.event\.label\.name == 'preview-mainmenu'/);
  assert.match(workflow, /github\.event\.pull_request\.head\.sha/);
  assert.match(
    workflow,
    /if:\s*github\.event_name != 'workflow_dispatch'[\s\S]*?require-ci-green/,
    "an opt-in pre-merge run must still require green CI on the exact head"
  );
});

test("automatic PlayMode concurrency cannot collapse unrelated PRs into main", () => {
  const group = workflow.match(/concurrency:\s*\n\s*group:\s*([^\n]+)/)?.[1] ?? "";
  assert.match(group, /github\.event\.pull_request\.number/);
  assert.match(group, /github\.event\.workflow_run\.pull_requests\[0\]\.number/);
  assert.match(group, /github\.event\.workflow_run\.id/);
  assert.doesNotMatch(
    group,
    /github\.event\.workflow_run\.head_branch/,
    "lossy workflow_run head_branch can report main for every PR and cause cross-PR cancellation"
  );
  assert.match(workflow, /cancel-in-progress:\s*true/);
});

test("workflow_run recovers the PR target from the authoritative source CI run", () => {
  assert.match(
    workflow,
    /SOURCE_RUN_ID:\s*\$\{\{\s*github\.event\.workflow_run\.id\s*\}\}/,
    "the chained workflow must retain the completed CI run id"
  );
  assert.match(
    workflow,
    /actions\/runs\/\$\{SOURCE_RUN_ID\}/,
    "workflow_run must re-read its source CI run because the event payload can lose PR association"
  );
  assert.match(workflow, /\.pull_requests\[0\]\.head\.sha/);
  assert.match(workflow, /\.pull_requests\[0\]\.head\.ref/);
  assert.match(workflow, /github\.event\.workflow_run\.head_sha/);
  assert.match(workflow, /github\.event\.workflow_run\.head_branch/);
  assert.doesNotMatch(
    workflow,
    /github\.event\.workflow_run\.pull_requests\[0\]\.head\.sha/,
    "the lossy event array must not be treated as the sole PR authority"
  );
});

test("PlayMode workflow proves that a complete Unity project exists before GameCI", () => {
  assert.match(
    workflow,
    /test -f ProjectSettings\/ProjectVersion\.txt/,
    "the workflow must fail with a precise checkout diagnostic before GameCI starts"
  );
  assert.match(workflow, /test -f Packages\/manifest\.json/);
  assert.match(workflow, /test -d Assets/);
  assert.match(workflow, /projectPath:\s*\./);
  assert.match(workflow, /requested_sha=/);
  assert.match(workflow, /requested_branch=/);
  assert.match(workflow, /actual_sha=/);
});

test("PlayMode cannot occupy a runner indefinitely", () => {
  assert.match(
    workflow,
    /exact-visuals-playmode:[\s\S]*?timeout-minutes:\s*25/,
    "the Unity PlayMode job needs a hard timeout so a stranded fixture fails closed"
  );
});

test("PlayMode never reuses cached C# compilation products", () => {
  const cacheBlock =
    workflow.match(/- name: Cache Library([\s\S]*?)(?=\n\s*- name:)/)?.[1] ?? "";

  assert.match(cacheBlock, /ProjectSettings\/ProjectVersion\.txt/);
  assert.match(cacheBlock, /Assets\/\*\*\/\*\.cs/);
  assert.match(cacheBlock, /Assets\/\*\*\/\*\.asmdef/);
  assert.match(cacheBlock, /Assets\/\*\*\/\*\.asmref/);
  assert.doesNotMatch(
    cacheBlock,
    /restore-keys:/,
    "a broad prefix restore can replay a test assembly built from removed source"
  );

  const invalidationBlock =
    workflow.match(
      /- name: Invalidate restored Unity compilation state([\s\S]*?)(?=\n\s*- name:)/
    )?.[1] ?? "";

  assert.match(invalidationBlock, /Library\/ScriptAssemblies/);
  assert.match(invalidationBlock, /Library\/Bee/);
  assert.match(invalidationBlock, /Library\/BuildCache/);
  assert.match(invalidationBlock, /Library\/BuildPlayerData/);
});

test("PlayMode failures always retain useful diagnostics", () => {
  assert.match(workflow, /artifactsPath:\s*artifacts\/playmode/);
  assert.match(workflow, /Collect Unity failure diagnostics/);
  assert.match(workflow, /if-no-files-found:\s*error/);
  assert.match(workflow, /retention-days:\s*7/);
});

test("Daily mission isolation owns and unloads only its unique test scene", () => {
  assert.match(
    missionFlow,
    /Scene\s+testScene\s*;/,
    "the fixture must retain the exact scene identity it owns"
  );
  assert.match(
    missionFlow,
    /SceneManager\.CreateScene\(\s*"DailyChallengeMissionFlowTests_"\s*\+\s*Guid\.NewGuid\(\)\.ToString\("N"\)\s*\)/,
    "the fixture needs a unique non-production scene name"
  );
  assert.match(
    missionFlow,
    /SceneManager\.SetActiveScene\(testScene\)/,
    "objects created by the fixture must enter the test-owned scene"
  );
  assert.match(
    missionFlow,
    /SceneManager\.UnloadSceneAsync\(testScene\)/,
    "teardown must unload exactly the scene retained by the fixture"
  );
  assert.doesNotMatch(
    missionFlow,
    /SceneManager\.sceneCount|SceneManager\.GetSceneAt/,
    "the fixture must not enumerate scenes owned by Unity Test Framework"
  );
  assert.doesNotMatch(
    missionFlow,
    /"SplashScene"|LoadSceneMode\.Single/,
    "component isolation must not start production splash lifecycle or replace runner scenes"
  );
});
