Merge all feature branches into the main branch, clean up, and push.

## Steps

1. **Identify branches**: List all local branches. Determine the main branch (prefer `main`, fall back to `master`). All other branches are feature branches.

2. **Ensure clean state**: Check for uncommitted changes on the current branch. If any, commit them first with a descriptive message.

3. **Switch to main branch**: `git checkout <main-branch>`

4. **Squash-merge each feature branch** (in numeric order if numbered):
   - For each feature branch: `git merge --squash <branch>`
   - Commit with message: `feat: <branch-name> (squash merge)`
   - If there are merge conflicts, resolve them and continue

5. **Delete feature branches**: After all merges complete, delete each feature branch: `git branch -D <branch>`

6. **Ensure remote exists**: Check if a GitHub remote is configured. If not, check if a repo exists on GitHub matching the directory name under the git user's account. If no remote exists, create the repo with `gh repo create` (private by default) and add the remote.

7. **Push to GitHub**: `git push -u origin <main-branch>`

8. **Report**: List what was merged, deleted, and pushed.

## Important

- Always confirm with the user before force-pushing or if conflicts arise
- Use squash merge (`git merge --squash`) to keep main branch history clean
- Delete feature branches only after successful merge
- If `gh` CLI is available, use it for repo creation; otherwise instruct the user
