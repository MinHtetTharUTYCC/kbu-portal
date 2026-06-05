Run git diff HEAD and git status, analyze all changes, then generate a single git command in this exact format:
git add . && git commit -m "<type>: <short summary>

- <specific change 1>
- <specific change 2>
- <specific change 3>
  ..." && git push
  Use conventional commit types (feat, fix, refactor, chore, etc.). Each bullet should describe a concrete, file-level or logic-level change. Do not execute — show me the command first for approval."
