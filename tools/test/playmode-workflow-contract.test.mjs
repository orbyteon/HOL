import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";

const workflowPath = ".github/workflows/playmode-tests.yml";
const workflow = fs.readFileSync(workflowPath, "utf8");

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

test("PlayMode workflow proves that a complete Unity project exists before GameCI", () => {
  assert.match(
    workflow,
    /test -f ProjectSettings\/ProjectVersion\.txt/,
    "the workflow must fail with a precise checkout diagnostic before GameCI starts"
  );
  assert.match(workflow, /test -f Packages\/manifest\.json/);
  assert.match(workflow, /test -d Assets/);
  assert.match(workflow, /projectPath:\s*\./);
});

test("PlayMode failures always retain useful diagnostics", () => {
  assert.match(workflow, /artifactsPath:\s*artifacts\/playmode/);
  assert.match(workflow, /Collect Unity failure diagnostics/);
  assert.match(workflow, /if-no-files-found:\s*error/);
  assert.match(workflow, /retention-days:\s*7/);
});
