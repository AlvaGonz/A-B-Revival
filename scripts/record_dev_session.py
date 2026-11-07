"""
Dev Session Recorder & Telemetry Analyzer (A&B Revival)

Captures and records live performance telemetry when the developer toggles Dev Mode ('I' or '~').
Saves detailed play session benchmarks to logs/dev_session.txt and compares metrics
against the automated runtime gate (scripts/test_perf_runtime.py).

Usage:
  python scripts/record_dev_session.py          # Extracts latest Dev Mode session from flashlog.txt
  python scripts/record_dev_session.py --watch  # Watches live telemetry in real-time as you play
  python scripts/record_dev_session.py --clear  # Clears flashlog.txt before starting a new test run
"""

import os
import sys
import time
import re
import argparse
from datetime import datetime

DEFAULT_FLASHLOG = os.path.expandvars(r"%APPDATA%\Macromedia\Flash Player\Logs\flashlog.txt")
LOGS_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "logs")
OUTPUT_FILE = os.path.join(LOGS_DIR, "dev_session.txt")

METRICS_REGEX = re.compile(
    r"\[PERF_METRICS\]\s+time=(\d+)s\s+fps=([\d\.]+)\s+frameDelta=(\d+)ms\s+logic=(\d+)ms\s+render=(\d+)ms\s+mem=([\d\.]+)MB(?:\s+bullets=(\d+))?(?:\s+particles=(\d+))?(?:\s+ships=(\d+))?(?:\s+drops=(\d+))?(?:\s+debris=(\d+))?(?:\s+dObjs=(\d+))?(?:\s+arrays=(\d+))?"
)
DEV_START_REGEX = re.compile(r"\[DEV_SESSION_START\](.*)")
DEV_END_REGEX = re.compile(r"\[DEV_SESSION_END\](.*)")
DEV_ENABLED_REGEX = re.compile(r"\[DEV_MODE\]\s+Enabled")
DEV_DISABLED_REGEX = re.compile(r"\[DEV_MODE\]\s+Disabled")


class MetricSample:
    def __init__(self, t, fps, frame_delta, logic_ms, render_ms, mem_mb, bullets, particles, ships, drops, debris, dobjs, arrays):
        self.time = int(t)
        self.fps = float(fps)
        self.frame_delta = int(frame_delta)
        self.logic_ms = int(logic_ms)
        self.render_ms = int(render_ms)
        self.mem_mb = float(mem_mb)
        self.bullets = int(bullets or 0)
        self.particles = int(particles or 0)
        self.ships = int(ships or 0)
        self.drops = int(drops or 0)
        self.debris = int(debris or 0)
        self.dobjs = int(dobjs or 0)
        self.arrays = int(arrays or 0)


def parse_line(line):
    m = METRICS_REGEX.search(line)
    if not m:
        return None
    return MetricSample(
        t=m.group(1),
        fps=m.group(2),
        frame_delta=m.group(3),
        logic_ms=m.group(4),
        render_ms=m.group(5),
        mem_mb=m.group(6),
        bullets=m.group(7),
        particles=m.group(8),
        ships=m.group(9),
        drops=m.group(10),
        debris=m.group(11),
        dobjs=m.group(12),
        arrays=m.group(13),
    )


def compute_statistics(samples, session_meta=""):
    if not samples:
        return "No performance samples recorded in this session."

    total_samples = len(samples)
    fps_vals = [s.fps for s in samples]
    logic_vals = [s.logic_ms for s in samples]
    render_vals = [s.render_ms for s in samples]
    mem_vals = [s.mem_mb for s in samples]
    bullet_vals = [s.bullets for s in samples]
    particle_vals = [s.particles for s in samples]
    dobj_vals = [s.dobjs for s in samples]

    avg_fps = sum(fps_vals) / total_samples
    min_fps = min(fps_vals)
    max_fps = max(fps_vals)

    sorted_fps = sorted(fps_vals)
    p1_index = max(0, int(total_samples * 0.01))
    fps_1percent_low = sorted_fps[p1_index]

    avg_logic = sum(logic_vals) / total_samples
    max_logic = max(logic_vals)

    avg_render = sum(render_vals) / total_samples
    max_render = max(render_vals)

    peak_mem = max(mem_vals)
    max_bullets = max(bullet_vals)
    max_particles = max(particle_vals)
    max_dobjs = max(dobj_vals)

    # Comparison against Automated Benchmark Gate baseline
    # (Automated gate requires Avg FPS >= 20.0, Peak Mem <= 80.0 MB)
    GATE_MIN_FPS = 20.0
    GATE_MAX_MEM = 80.0

    passed_fps_gate = avg_fps >= GATE_MIN_FPS
    passed_mem_gate = peak_mem <= GATE_MAX_MEM

    lines = []
    lines.append("================================================================================")
    lines.append("            A&B REVIVAL - DEVELOPER PLAY SESSION TELEMETRY REPORT")
    lines.append("================================================================================")
    lines.append(f"Recorded Date      : {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    if session_meta:
        lines.append(f"Session Metadata   : {session_meta.strip()}")
    lines.append(f"Samples Collected  : {total_samples} snapshots")
    lines.append(f"Session Duration   : ~{samples[-1].time - samples[0].time if total_samples > 1 else 0} seconds")
    lines.append("--------------------------------------------------------------------------------")
    lines.append("METRIC                           RECORDED DEV SESSION     RUNTIME GATE BASELINE")
    lines.append("--------------------------------------------------------------------------------")
    lines.append(f"Average FPS                    : {avg_fps:6.1f} FPS             >= {GATE_MIN_FPS:.1f} FPS ({'PASS' if passed_fps_gate else 'FAIL'})")
    lines.append(f"Min FPS (1% Low)               : {fps_1percent_low:6.1f} FPS             --")
    lines.append(f"Absolute Min FPS               : {min_fps:6.1f} FPS             --")
    lines.append(f"Max FPS                        : {max_fps:6.1f} FPS             --")
    lines.append(f"Avg Logic Execution Time       : {avg_logic:6.1f} ms              --")
    lines.append(f"Peak Logic Time                : {max_logic:6.1f} ms              --")
    lines.append(f"Avg Render Time                : {avg_render:6.1f} ms              --")
    lines.append(f"Peak Render Time               : {max_render:6.1f} ms              --")
    lines.append(f"Peak Memory Usage              : {peak_mem:6.1f} MB             <= {GATE_MAX_MEM:.1f} MB ({'PASS' if passed_mem_gate else 'WARN'})")
    lines.append(f"Peak Active Bullets            : {max_bullets:6d}                 --")
    lines.append(f"Peak Active Particles          : {max_particles:6d}                 --")
    lines.append(f"Peak Display Objects           : {max_dobjs:6d}                 --")
    lines.append("--------------------------------------------------------------------------------")
    lines.append("DIAGNOSTIC ANALYSIS:")
    if avg_fps < 30.0:
        lines.append(f"  * [LOW FPS ALERT]: Average FPS is {avg_fps:.1f}. High load in combat detected.")
    if max_render > 20:
        lines.append(f"  * [RENDER BOUND]: Peak render ms reached {max_render}ms. Software vector glow/rasterization is saturating the frame budget.")
    if max_logic > 15:
        lines.append(f"  * [SIMULATION OVERLOAD]: Peak logic reached {max_logic}ms. Multi-substep accumulator triggers when frame rates drop.")
    if passed_fps_gate and passed_mem_gate:
        lines.append("  * [STATUS]: Play session passed all primary runtime quality thresholds.")
    lines.append("================================================================================")
    lines.append("")
    lines.append("RAW TELEMETRY TIMELINE:")
    lines.append("Time(s) | FPS   | FrameDelta | Logic(ms) | Render(ms) | Mem(MB) | Bullets | Particles | DObjs")
    lines.append("----------------------------------------------------------------------------------------")
    for s in samples:
        lines.append(f"{s.time:6d}s | {s.fps:5.1f} | {s.frame_delta:8d}ms | {s.logic_ms:7d}ms | {s.render_ms:8d}ms | {s.mem_mb:5.1f}MB | {s.bullets:7d} | {s.particles:9d} | {s.dobjs:5d}")

    return "\n".join(lines)


def extract_sessions_from_file(log_path):
    if not os.path.exists(log_path):
        print(f"Error: Log file not found at {log_path}")
        return []

    with open(log_path, "r", encoding="utf-8", errors="ignore") as f:
        lines = f.readlines()

    sessions = []
    current_session = []
    current_meta = ""
    in_dev_mode = False

    for line in lines:
        if DEV_START_REGEX.search(line):
            in_dev_mode = True
            current_meta = DEV_START_REGEX.search(line).group(1)
            current_session = []
            continue

        if DEV_ENABLED_REGEX.search(line):
            in_dev_mode = True
            current_session = []
            continue

        if DEV_END_REGEX.search(line) or DEV_DISABLED_REGEX.search(line):
            if current_session:
                sessions.append((current_meta, current_session))
            in_dev_mode = False
            current_session = []
            current_meta = ""
            continue

        sample = parse_line(line)
        if sample:
            if in_dev_mode or not sessions:
                current_session.append(sample)

    if current_session:
        sessions.append((current_meta, current_session))

    return sessions


def run_batch_extract(log_path):
    os.makedirs(LOGS_DIR, exist_ok=True)
    sessions = extract_sessions_from_file(log_path)
    if not sessions:
        print(f"No dev mode performance sessions found in {log_path}.")
        print("Tip: Start the game, press 'I' or '~' to toggle dev mode, play for a few seconds, then re-run this script.")
        return False

    meta, latest_samples = sessions[-1]
    report = compute_statistics(latest_samples, meta)

    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        f.write(report)

    print(f"\n[OK] Recorded {len(latest_samples)} samples to: {OUTPUT_FILE}")
    print("\n" + report.split("RAW TELEMETRY TIMELINE:")[0])
    return True


def run_watch_mode(log_path):
    os.makedirs(LOGS_DIR, exist_ok=True)
    print(f"\n[WATCH MODE] Monitoring {log_path} for active Dev Mode sessions...")
    print("Press Ctrl+C to stop watching.\n")

    if not os.path.exists(log_path):
        open(log_path, "a").close()

    with open(log_path, "r", encoding="utf-8", errors="ignore") as f:
        f.seek(0, os.SEEK_END)
        current_samples = []
        current_meta = ""
        in_dev_mode = False

        while True:
            line = f.readline()
            if not line:
                time.sleep(0.2)
                continue

            if DEV_START_REGEX.search(line):
                in_dev_mode = True
                current_meta = DEV_START_REGEX.search(line).group(1)
                current_samples = []
                print(f"[DEV_MODE] Session Started: {current_meta}")
                continue

            if DEV_ENABLED_REGEX.search(line):
                in_dev_mode = True
                current_samples = []
                print("[DEV_MODE] Activated. Recording samples...")
                continue

            if DEV_END_REGEX.search(line) or DEV_DISABLED_REGEX.search(line):
                if current_samples:
                    report = compute_statistics(current_samples, current_meta)
                    with open(OUTPUT_FILE, "w", encoding="utf-8") as out:
                        out.write(report)
                    print(f"\n[DEV_MODE] Session ended. Saved {len(current_samples)} samples to {OUTPUT_FILE}")
                in_dev_mode = False
                current_samples = []
                continue

            sample = parse_line(line)
            if sample:
                current_samples.append(sample)
                print(f"  [METRICS] {sample.time}s | FPS: {sample.fps:4.1f} | Logic: {sample.logic_ms:2d}ms | Render: {sample.render_ms:2d}ms | Mem: {sample.mem_mb:5.1f}MB | Bullets: {sample.bullets:3d} | Particles: {sample.particles:4d}")
                # Save partial report incrementally
                report = compute_statistics(current_samples, current_meta)
                with open(OUTPUT_FILE, "w", encoding="utf-8") as out:
                    out.write(report)


def clear_log(log_path):
    if os.path.exists(log_path):
        with open(log_path, "w", encoding="utf-8") as f:
            f.truncate(0)
        print(f"[OK] Cleared log file: {log_path}")
    else:
        print(f"Log file {log_path} does not exist.")


def main():
    parser = argparse.ArgumentParser(description="Record and analyze dev mode telemetry.")
    parser.add_argument("--log", default=DEFAULT_FLASHLOG, help="Path to flashlog.txt")
    parser.add_argument("--watch", "-w", action="store_true", help="Live watch mode")
    parser.add_argument("--clear", "-c", action="store_true", help="Clear flashlog.txt before recording")
    args = parser.parse_args()

    if args.clear:
        clear_log(args.log)

    if args.watch:
        try:
            run_watch_mode(args.log)
        except KeyboardInterrupt:
            print("\nStopped watcher.")
    else:
        run_batch_extract(args.log)


if __name__ == "__main__":
    main()

