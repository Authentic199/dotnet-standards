#!/usr/bin/env python3
"""Read a Claude Code session transcript and report what a trial run actually did.

The measurement this answers — did a session route through dotnet-standards —
is the one a session is least reliable about reporting. On 2026-08-02 a session
believed it had reviewed a branch properly; the transcript said otherwise. So
nothing here reads prose. It reads three machine-written facts:

  1. which hooks fired, identified by the text they emitted, not by event name
  2. every Skill-tool load, in order
  3. every subagent spawn and its subagent_type

The transcript is written per event, on disk, as the session runs, so it
survives context compaction and needs no cooperation from the session. Run this
AFTER the trial ends. Never show the trial session this file.

Usage:
    python trial-extract.py                      # newest session, this project
    python trial-extract.py <project-dir>        # newest session of that project
    python trial-extract.py <path-to.jsonl>      # one exact session
    python trial-extract.py <project-dir> <n>    # nth newest (1 = newest)

<project-dir> is the encoded folder under ~/.claude/projects/, e.g.
D--ALTA-Project-TWOH-ops-service — or just the repository path itself, which is
encoded for you.
"""

import collections
import io
import json
import os
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

# A hook is identified by what it emitted. The event name is not enough: two
# dotnet-standards hooks share PreToolUse, and other plugins share every event.
HOOK_SIGNATURES = [
    ("router-nudge", "dotnet-standards is installed and this working directory"),
    ("fleet-nudge", "A subagent is being spawned in a .NET solution"),
    ("process-handback", "A Superpowers process skill is being loaded"),
    ("test-report-nudge", "A `dotnet test` run has just completed"),
    ("superpowers-check", "Superpowers is not installed"),
    ("superpowers:using-superpowers", "You have superpowers."),
]

SPECIALIST_AGENTS = {
    "dotnet-code-reviewer",
    "dotnet-architecture-reviewer",
    "dotnet-security-reviewer",
    "dotnet-performance-reviewer",
    "dotnet-unit-tester",
    "dotnet-integration-tester",
}

FLOW_SKILLS = {
    "dotnet-standards:dotnet-feature-flow",
    "dotnet-standards:dotnet-review-flow",
}

REVIEW_SKILLS = {
    "dotnet-standards:dotnet-code-review",
    "dotnet-standards:dotnet-architecture-review",
    "dotnet-standards:dotnet-security-review",
    "dotnet-standards:dotnet-performance-review",
    "dotnet-standards:dotnet-review-flow",
}


def projects_root():
    return os.path.join(
        os.environ.get("CLAUDE_CONFIG_DIR") or os.path.expanduser("~/.claude"),
        "projects",
    )


def encode_project_path(path):
    """~/.claude/projects encodes a repository path by replacing separators."""
    return os.path.abspath(path).replace("\\", "-").replace("/", "-").replace(":", "-")


def resolve_transcript(arg, nth):
    if arg and arg.endswith(".jsonl"):
        return arg
    root = projects_root()
    if not arg:
        candidate = encode_project_path(os.getcwd())
    elif os.path.isdir(os.path.join(root, arg)):
        candidate = arg
    else:
        candidate = encode_project_path(arg)
    folder = os.path.join(root, candidate)
    if not os.path.isdir(folder):
        sys.exit(
            "No transcript folder for %r.\nLooked in %s\nAvailable:\n  %s"
            % (arg or os.getcwd(), folder, "\n  ".join(sorted(os.listdir(root))))
        )
    files = [
        os.path.join(folder, f) for f in os.listdir(folder) if f.endswith(".jsonl")
    ]
    if not files:
        sys.exit("No .jsonl transcripts in %s" % folder)
    files.sort(key=os.path.getmtime, reverse=True)
    if nth > len(files):
        sys.exit("Only %d transcripts in %s" % (len(files), folder))
    return files[nth - 1]


def identify_hook(text):
    for name, signature in HOOK_SIGNATURES:
        if signature in text:
            return name
    return None


def main():
    args = [a for a in sys.argv[1:]]
    nth = 1
    if args and args[-1].isdigit():
        nth = int(args.pop())
    path = resolve_transcript(args[0] if args else None, nth)

    hooks = []           # (name, event) in order of first appearance
    seen_hooks = set()
    other_hook_events = collections.Counter()
    skills = []          # skill names in order
    spawns = []          # (subagent_type, description)
    prompts = []

    for line in io.open(path, encoding="utf-8", errors="replace"):
        line = line.strip()
        if not line:
            continue
        try:
            record = json.loads(line)
        except ValueError:
            continue

        attachment = record.get("attachment")
        if isinstance(attachment, dict) and str(attachment.get("type", "")).startswith("hook"):
            content = attachment.get("content") or attachment.get("stdout") or ""
            if isinstance(content, list):
                content = "\n".join(str(c) for c in content)
            name = identify_hook(str(content))
            event = attachment.get("hookEvent") or attachment.get("hookName") or "?"
            if name:
                if name not in seen_hooks:
                    seen_hooks.add(name)
                    hooks.append((name, event))
            else:
                other_hook_events[event] += 1

        message = record.get("message") or {}
        content = message.get("content")
        if isinstance(content, str) and message.get("role") == "user":
            prompts.append(content)
        for block in content if isinstance(content, list) else []:
            if not isinstance(block, dict):
                continue
            if block.get("type") == "text" and message.get("role") == "user":
                prompts.append(block.get("text") or "")
            if block.get("type") != "tool_use":
                continue
            name = block.get("name")
            payload = block.get("input") or {}
            if name == "Skill":
                skills.append(payload.get("skill") or "?")
            elif name in ("Task", "Agent"):
                spawns.append(
                    (
                        payload.get("subagent_type") or "(default)",
                        (payload.get("description") or "")[:60],
                    )
                )

    session_id = os.path.splitext(os.path.basename(path))[0]
    print("Transcript : %s" % path)
    print("Session id : %s" % session_id)
    print()

    print("== HOOKS THAT FIRED (identified by emitted text) ==")
    if hooks:
        for name, event in hooks:
            print("  %-32s %s" % (name, event))
    else:
        print("  none identified")
    if other_hook_events:
        print("  (other hook output, unrecognised: %s)"
              % ", ".join("%s x%d" % kv for kv in sorted(other_hook_events.items())))
    print()

    print("== SKILLS LOADED, IN ORDER (%d) ==" % len(skills))
    if skills:
        for i, skill in enumerate(skills, 1):
            print("  %2d. %s" % (i, skill))
    else:
        print("  none")
    print()

    print("== SUBAGENT SPAWNS (%d) ==" % len(spawns))
    by_type = collections.Counter(t for t, _ in spawns)
    for agent_type, count in by_type.most_common():
        print("  %-32s x%d" % (agent_type, count))
    print()

    print("== VERDICT ==")
    loaded = set(skills)
    # A subagent_type may or may not carry its plugin prefix — both forms occur.
    bare = lambda t: t.split(":")[-1]
    used_specialists = sorted({bare(t) for t in by_type} & SPECIALIST_AGENTS)
    generic = sum(c for t, c in by_type.items()
                  if bare(t) in ("general-purpose", "(default)", "claude"))
    entered_flow = sorted(loaded & FLOW_SKILLS)
    command_used = [p for p in prompts if "/dotnet-feature" in p or "/dotnet-review" in p]

    def verdict(label, ok, detail=""):
        print("  [%s] %-46s %s" % ("x" if ok else " ", label, detail))

    verdict("process-handback fired", "process-handback" in seen_hooks)
    verdict("fleet-nudge fired", "fleet-nudge" in seen_hooks)
    verdict("a dotnet-standards flow was entered",
            bool(entered_flow) or bool(command_used),
            ", ".join(entered_flow) or ("command" if command_used else ""))
    verdict("a review rubric or the review flow was loaded",
            bool(loaded & REVIEW_SKILLS), ", ".join(sorted(loaded & REVIEW_SKILLS)))
    verdict("specialist agents were spawned",
            bool(used_specialists), ", ".join(used_specialists))
    verdict("every spawn was a specialist", generic == 0,
            "%d generic spawn(s) — read their descriptions above: generic is "
            "correct for implementation, wrong for review" % generic if generic else "")
    print()
    print("  Hooks firing but nothing else ticked = the nudge was heard and ignored.")
    print("  Nothing ticked at all = check the markers under /tmp/dotnet-standards/;")
    print("  no marker means the script never ran (plumbing), a marker means it did.")


if __name__ == "__main__":
    main()
