#!/usr/bin/env python3
"""Automated Runtime Performance Test Gate & Benchmark Harness.

Executes the compiled SWF in standalone Flash Player Debug mode, captures
per-frame telemetry from flashlog.txt, parses real-time metrics (FPS, delta time,
logic/render execution durations, memory, entity counts), asserts quality gates,
and reports results.

Fulfills requirements for Issue #90 (Automated Runtime Test Harness and Gates).
"""

import argparse
import os
import re
import subprocess
import sys
import time

DEFAULT_PLAYER_PATHS = [
    r"C:\Dev\Flashpoint Infinity 14.0.3\FPSoftware\Flash\flashplayer11_9r900_152_win_sa_debug.exe",
    r"C:\Dev\Flashpoint Infinity 14.0.3\FPSoftware\Flash\flashplayer_32_sa.exe",
]

DEFAULT_SWF_PATH = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    "game",
    "a&b_space_mod.swf",
)

LOG_PATH = os.path.expandvars(
    r"%APPDATA%\Macromedia\Flash Player\Logs\flashlog.txt"
)


def find_player(custom_path=None):
    if custom_path and os.path.isfile(custom_path):
        return custom_path
    for p in DEFAULT_PLAYER_PATHS:
        if os.path.isfile(p):
            return p
    return None


def ensure_mm_cfg():
    cfg_dir = os.path.expandvars(r"%APPDATA%\Macromedia\Flash Player")
    cfg_path = os.path.join(cfg_dir, "mm.cfg")
    os.makedirs(cfg_dir, exist_ok=True)
    logs_dir = os.path.dirname(LOG_PATH)
    os.makedirs(logs_dir, exist_ok=True)

    expected_content = (
        "ErrorReportingEnable=1\n"
        "TraceOutputFileEnable=1\n"
        f"TraceOutputFileName={LOG_PATH}\n"
    )
    if not os.path.exists(cfg_path):
        with open(cfg_path, "w", encoding="utf-8") as f:
            f.write(expected_content)


def clear_log():
    if os.path.exists(LOG_PATH):
        try:
            with open(LOG_PATH, "w", encoding="utf-8") as f:
                f.write("")
        except Exception as e:
            print(f"[WARN] Failed to clear flashlog.txt: {e}")


def run_benchmark(player_path, swf_path, duration_sec):
    ensure_mm_cfg()
    clear_log()

    print(f"[BENCHMARK] Executing SWF: {swf_path}")
    print(f"[BENCHMARK] Using Player: {player_path}")
    print(f"[BENCHMARK] Duration: {duration_sec}s")

    cmd = [player_path, swf_path]
    proc = subprocess.Popen(cmd)

    start_time = time.time()
    try:
        while time.time() - start_time < duration_sec:
            if proc.poll() is not None:
                print(f"[WARN] Player process exited early with code {proc.returncode}")
                break
            time.sleep(0.5)
    finally:
        if proc.poll() is None:
            proc.terminate()
            try:
                proc.wait(timeout=2.0)
            except subprocess.TimeoutExpired:
                proc.kill()

    print("[BENCHMARK] Execution completed. Analyzing telemetry...")


def parse_telemetry():
    if not os.path.exists(LOG_PATH):
        return {"error": f"Log file not found at {LOG_PATH}"}

    with open(LOG_PATH, "r", encoding="utf-8", errors="ignore") as f:
        lines = f.readlines()

    metric_pattern = re.compile(
        r"\[PERF_METRICS\]\s+time=(\d+)s\s+fps=([0-9.]+)\s+frameDelta=(\d+)ms\s+logic=(\d+)ms\s+render=(\d+)ms\s+mem=([0-9.]+)MB(?:\s+bullets=(\d+))?(?:\s+particles=(\d+))?(?:\s+ships=(\d+))?(?:\s+drops=(\d+))?(?:\s+debris=(\d+))?(?:\s+dObjs=(\d+))?(?:\s+arrays=(\d+))?"
    )

    metrics = []
    fatal_errors = []

    for line in lines:
        stripped = line.strip()
        m = metric_pattern.search(stripped)
        if m:
            metrics.append({
                "time": int(m.group(1)),
                "fps": float(m.group(2)),
                "frameDelta": int(m.group(3)),
                "logic": int(m.group(4)),
                "render": int(m.group(5)),
                "mem": float(m.group(6)),
                "bullets": int(m.group(7) or 0),
                "particles": int(m.group(8) or 0),
                "ships": int(m.group(9) or 0),
                "drops": int(m.group(10) or 0),
                "debris": int(m.group(11) or 0),
                "dObjs": int(m.group(12) or 0),
                "arrays": int(m.group(13) or 0),
            })
        elif any(err in stripped for err in ["TypeError", "RangeError", "ReferenceError", "VerifyError", "SecurityError"]):
            fatal_errors.append(stripped)

    return {
        "raw_lines_count": len(lines),
        "metrics": metrics,
        "fatal_errors": fatal_errors,
        "raw_lines": lines,
    }


def evaluate_gates(result, min_fps, max_mem):
    fatal_errors = result.get("fatal_errors", [])
    metrics = result.get("metrics", [])

    print("\n" + "=" * 65)
    print("           A&B REVIVAL RUNTIME PERFORMANCE GATE REPORT")
    print("=" * 65)

    if fatal_errors:
        print("[FAIL] Fatal ActionScript Runtime Errors Encountered:")
        for err in fatal_errors:
            print(f"  - {err}")
        print("=" * 65)
        return False

    if not metrics:
        print(f"[INFO] Process ran successfully ({result.get('raw_lines_count', 0)} log entries).")
        # Check if basic startup succeeded
        data_loaded = any("dataLoaded" in l for l in result.get("raw_lines", []))
        if data_loaded:
            print("[PASS] Core SWF initialized cleanly (dataLoaded verified).")
            print("[NOTE] Simulation did not reach a 180-frame metric cycle (increase --duration to >= 6s for full telemetry).")
            print("=" * 65)
            return True
        else:
            print("[FAIL] SWF did not reach dataLoaded initialization.")
            print("=" * 65)
            return False

    fps_vals = [m["fps"] for m in metrics]
    logic_vals = [m["logic"] for m in metrics]
    render_vals = [m["render"] for m in metrics]
    mem_vals = [m["mem"] for m in metrics]

    avg_fps = sum(fps_vals) / len(fps_vals)
    min_observed_fps = min(fps_vals)
    max_observed_fps = max(fps_vals)
    avg_logic = sum(logic_vals) / len(logic_vals)
    avg_render = sum(render_vals) / len(render_vals)
    peak_mem = max(mem_vals)
    last_sample = metrics[-1]

    print(f" Samples Collected : {len(metrics)} telemetry points")
    print(f" Framerate (FPS)   : Avg = {avg_fps:.1f} | Min = {min_observed_fps:.1f} | Max = {max_observed_fps:.1f} (Gate >= {min_fps:.1f})")
    print(f" Execution Timing  : Avg Logic = {avg_logic:.1f}ms | Avg Render = {avg_render:.1f}ms")
    print(f" Memory Footprint  : Peak = {peak_mem:.2f} MB (Gate <= {max_mem:.1f} MB)")
    print(f" Live Entity Counts: Bullets={last_sample['bullets']} | Particles={last_sample['particles']} | DisplayObjects={last_sample['dObjs']} | Arrays={last_sample['arrays']}")
    print("-" * 65)

    passed = True
    if avg_fps < min_fps:
        print(f"[FAIL] Average FPS ({avg_fps:.1f}) is below minimum threshold ({min_fps:.1f})")
        passed = False
    else:
        print(f"[PASS] Framerate gate passed: {avg_fps:.1f} FPS >= {min_fps:.1f} FPS")

    if peak_mem > max_mem:
        print(f"[FAIL] Peak memory ({peak_mem:.2f} MB) exceeds maximum threshold ({max_mem:.1f} MB)")
        passed = False
    else:
        print(f"[PASS] Memory gate passed: {peak_mem:.2f} MB <= {max_mem:.1f} MB")

    print("=" * 65)
    return passed


def main():
    parser = argparse.ArgumentParser(description="A&B Revival Runtime Performance Gate")
    parser.add_argument("--duration", type=int, default=8, help="Run duration in seconds")
    parser.add_argument("--min-fps", type=float, default=20.0, help="Minimum average FPS threshold")
    parser.add_argument("--max-mem", type=float, default=80.0, help="Maximum memory threshold in MB")
    parser.add_argument("--player", type=str, default=None, help="Path to standalone Flash Player")
    parser.add_argument("--swf", type=str, default=DEFAULT_SWF_PATH, help="Path to SWF")

    args = parser.parse_args()

    player = find_player(args.player)
    if not player:
        print("[ERROR] No Flash Player standalone found on system.")
        sys.exit(1)

    if not os.path.isfile(args.swf):
        print(f"[ERROR] SWF not found at {args.swf}")
        sys.exit(1)

    run_benchmark(player, args.swf, args.duration)
    result = parse_telemetry()
    passed = evaluate_gates(result, args.min_fps, args.max_mem)

    sys.exit(0 if passed else 1)


if __name__ == "__main__":
    main()

