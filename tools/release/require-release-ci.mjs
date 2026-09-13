import { pathToFileURL } from 'node:url';

export function requireSuccessfulMainPush(runs, sha) {
  const matching = runs.filter(run => run.head_sha === sha &&
    run.head_branch === 'main' && run.event === 'push');
  matching.sort((a, b) => Number(b.id) - Number(a.id));
  const latest = matching[0];
  if (!latest || latest.status !== 'completed' || latest.conclusion !== 'success')
    throw new Error(`The latest main push validation for ${sha} must be completed and successful.`);
  return latest.id;
}

export async function verifyRelease(env, request = fetch) {
  const sha = env.RELEASE_SHA;
  if (env.GITHUB_REF !== 'refs/heads/main' || !/^[a-f0-9]{40}$/.test(sha || ''))
    throw new Error('Release validation requires an exact main commit SHA.');
  if (!/^[\w.-]+\/[\w.-]+$/.test(env.GITHUB_REPOSITORY || '') || !env.GH_TOKEN)
    throw new Error('Repository and read-only Actions token are required.');
  const results = [];
  for (const workflow of ['ci.yml', 'production-visual-integrity.yml']) {
    const url = `https://api.github.com/repos/${env.GITHUB_REPOSITORY}/actions/workflows/${workflow}/runs?head_sha=${sha}&event=push&branch=main&per_page=100`;
    const response = await request(url, {
      headers: { Authorization: `Bearer ${env.GH_TOKEN}`, Accept: 'application/vnd.github+json' },
      signal: AbortSignal.timeout(30000),
    });
    if (!response.ok) throw new Error(`Cannot verify ${workflow}: HTTP ${response.status}`);
    const data = await response.json();
    if (!Array.isArray(data.workflow_runs)) throw new Error(`Missing runs for ${workflow}`);
    results.push({ workflow, run: requireSuccessfulMainPush(data.workflow_runs, sha) });
  }
  return results;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try { console.log(JSON.stringify(await verifyRelease(process.env))); }
  catch (error) { console.error(`::error::${error.message}`); process.exitCode = 1; }
}
