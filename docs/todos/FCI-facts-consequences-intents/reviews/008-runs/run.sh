#!/usr/bin/env bash
# usage: run.sh <prompt-file> <out-file>   (run from an empty directory)
claude -p "$(cat "$1")" --permission-mode dontAsk --strict-mcp-config --output-format stream-json --verbose \
  --allowedTools "Skill" "Read(~/.claude/skills/**)" "Glob(~/.claude/skills/**)" "Grep(~/.claude/skills/**)" \
  --disallowedTools "Bash" "PowerShell" "Agent" "Task" "WebFetch" "WebSearch" "Write" "Edit" "NotebookEdit" \
    "LSP" "SendMessage" "ListAgents" "Workflow" "RemoteTrigger" "CronCreate" "PushNotification" "SendUserFile" "Monitor" "EnterWorktree" \
    "Read(~/source/**)" "Read(//c/Users/KeithVoels/source/**)" "Read(//c/src/**)" \
    "Read(~/AppData/**)" "Read(//c/Users/KeithVoels/AppData/**)" \
    "Read(~/.claude/projects/**)" "Read(~/.claude/todos/**)" "Read(~/.claude/plans/**)" \
  < /dev/null > "$2" 2>&1
