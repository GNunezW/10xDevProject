#!/usr/bin/env python3
"""afterFileEdit: lint (dotnet format) + typecheck (dotnet build) for C# edits."""
from __future__ import annotations

import json
import os
import subprocess
import sys
from pathlib import Path

MAX_CONTEXT = 8000
HOOK_DIR = Path(__file__).resolve().parent
LOG_PATH = HOOK_DIR / "after-file-edit.log"
PATH_PREFIX = "/opt/homebrew/bin:/usr/local/bin:/usr/bin:/bin"


def emit(payload: dict) -> None:
    sys.stdout.write(json.dumps(payload, ensure_ascii=False))
    sys.stdout.write("\n")


def log(line: str) -> None:
    sys.stderr.write(line + "\n")
    try:
        with LOG_PATH.open("a", encoding="utf-8") as fh:
            fh.write(line + "\n")
    except OSError:
        pass


def run(cmd: list[str], cwd: Path) -> tuple[int, str]:
    env = os.environ.copy()
    env["PATH"] = f"{PATH_PREFIX}:{env.get('PATH', '')}"
    proc = subprocess.run(
        cmd,
        cwd=cwd,
        text=True,
        capture_output=True,
        timeout=90,
        env=env,
    )
    out = "\n".join(part for part in (proc.stdout, proc.stderr) if part).strip()
    return proc.returncode, out


def main() -> int:
    raw = sys.stdin.read()
    try:
        data = json.loads(raw) if raw.strip() else {}
    except json.JSONDecodeError:
        emit({})
        return 0

    file_path = data.get("file_path") or ""
    roots = data.get("workspace_roots") or []
    repo = Path(roots[0]) if roots else Path.cwd()

    if not str(file_path).lower().endswith(".cs"):
        log(f"afterFileEdit skip (not .cs): {file_path}")
        emit({})
        return 0

    abs_file = Path(file_path)
    try:
        rel = abs_file.resolve().relative_to(repo.resolve())
    except ValueError:
        rel = abs_file

    rel_s = str(rel).replace("\\", "/")
    if rel_s.startswith("plan-zajec-uczelnia.Tests/"):
        csproj = repo / "plan-zajec-uczelnia.Tests" / "plan-zajec-uczelnia.Tests.csproj"
    elif rel_s.startswith("plan-zajec-uczelnia/"):
        csproj = repo / "plan-zajec-uczelnia" / "plan-zajec-uczelnia.csproj"
    else:
        log(f"afterFileEdit skip (outside C# projects): {rel_s}")
        emit({})
        return 0

    log(f"afterFileEdit lint+typecheck: {rel_s}")

    lint_code, lint_out = run(
        [
            "dotnet",
            "format",
            str(csproj),
            "--include",
            rel_s,
            "--verify-no-changes",
            "--severity",
            "warn",
        ],
        repo,
    )
    type_code, type_out = run(
        ["dotnet", "build", str(csproj), "--nologo", "-v", "q"],
        repo,
    )

    lint_ok = lint_code == 0
    type_ok = type_code == 0
    log(f"afterFileEdit lint={'ok' if lint_ok else 'FAIL'} typecheck={'ok' if type_ok else 'FAIL'}")

    if lint_ok and type_ok:
        emit({})
        return 0

    parts = [
        "afterFileEdit hook failed. Fix the file you just edited.",
        f"File: {rel_s}",
        "",
        f"lint (dotnet format --verify-no-changes) exit {lint_code}:",
        lint_out or "(no output)",
        "",
        f"typecheck (dotnet build) exit {type_code}:",
        type_out or "(no output)",
    ]
    context = "\n".join(parts)
    if len(context) > MAX_CONTEXT:
        context = context[:MAX_CONTEXT] + "\n…(truncated)"
    emit({"additional_context": context})
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except subprocess.TimeoutExpired:
        emit(
            {
                "additional_context": "afterFileEdit hook timed out running lint/typecheck. Re-run `dotnet format --verify-no-changes` and `dotnet build` locally."
            }
        )
        raise SystemExit(0)
    except Exception as exc:  # hook must fail open with JSON
        log(f"afterFileEdit hook error: {exc}")
        emit({})
        raise SystemExit(0)
