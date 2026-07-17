#!/usr/bin/env python3
"""Simple, cheerful kid quiz audio — one clear melody family, short SFX."""
from __future__ import annotations

import math
import struct
import wave
from pathlib import Path

SR = 44100
OUT = Path(__file__).resolve().parents[1] / "Assets" / "Audio" / "BuiltIn"
TAU = 2 * math.pi

# C major — bright, familiar
C3, E3, F3, G3 = 130.81, 164.81, 174.61, 196.0
C4, D4, E4, G4, A4 = 261.63, 293.66, 329.63, 392.0, 440.0
C5, D5, E5, G5, A5, B5, C6 = 523.25, 587.33, 659.25, 783.99, 880.0, 987.77, 1046.5


def write_wav(path: Path, samples: list[float], sr: int = SR) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        frames = bytearray()
        for s in samples:
            v = max(-1.0, min(1.0, s))
            frames += struct.pack("<h", int(v * 32767 * 0.82))
        w.writeframes(frames)


def env(t: float, attack: float, release: float, dur: float) -> float:
    if t < attack:
        return t / max(attack, 1e-6)
    if t > dur - release:
        return max(0.0, (dur - t) / max(release, 1e-6))
    return 1.0


def render(dur: float, fn) -> list[float]:
    n = max(1, int(SR * dur))
    return [fn(i / SR) for i in range(n)]


def toy(freq: float, t: float, amp: float = 1.0) -> float:
    """Bright glockenspiel — a touch of extra sparkle (3rd harmonic) for a more cheerful ring."""
    d = math.exp(-t * 10.5)
    return amp * d * (
        0.72 * math.sin(TAU * freq * t)
        + 0.18 * math.sin(TAU * freq * 2.0 * t)
        + 0.08 * math.sin(TAU * freq * 3.0 * t)
    )


def note(freq: float, beats: float, bpm: float, amp: float = 0.55) -> list[float]:
    dur = beats * 60.0 / bpm
    plen = min(dur * 0.75, 0.22)
    s = render(plen, lambda t: toy(freq, t, amp) * env(t, 0.002, 0.05, plen))
    rest = dur - plen
    if rest > 0:
        s.extend([0.0] * int(SR * rest))
    return s


def silence(beats: float, bpm: float) -> list[float]:
    return [0.0] * int(SR * beats * 60.0 / bpm)


def pad(freq: float, dur: float, amp: float = 0.16) -> list[float]:
    """Soft, gently-detuned two-oscillator bass pad — fills out the melody underneath."""
    attack = min(0.25, dur * 0.3)
    release = min(0.45, dur * 0.3)

    def fn(t: float) -> float:
        e = env(t, attack, release, dur)
        s = 0.6 * math.sin(TAU * freq * t) + 0.4 * math.sin(TAU * (freq * 1.003) * t)
        return amp * e * s

    return render(dur, fn)


def mix(*layers: list[float]) -> list[float]:
    n = max(len(layer) for layer in layers)
    out = [0.0] * n
    for layer in layers:
        for i, v in enumerate(layer):
            out[i] += v
    return out


def melody_loop(bpm: float, pattern: list[tuple[float, float]], repeats: int) -> list[float]:
    out: list[float] = []
    for _ in range(repeats):
        for freq, beats in pattern:
            if freq <= 0:
                out.extend(silence(beats, bpm))
            else:
                out.extend(note(freq, beats, bpm))
    return out


def bass_loop(bpm: float, pattern: list[tuple[float, float]], repeats: int) -> list[float]:
    out: list[float] = []
    for _ in range(repeats):
        for freq, beats in pattern:
            out.extend(pad(freq, beats * 60.0 / bpm))
    return out


def melody_with_bass(bpm: float, pattern: list[tuple[float, float]], repeats: int,
                      bass_pattern: list[tuple[float, float]]) -> list[float]:
    return mix(melody_loop(bpm, pattern, repeats), bass_loop(bpm, bass_pattern, repeats))


def concat(*parts: list[float]) -> list[float]:
    o: list[float] = []
    for p in parts:
        o.extend(p)
    return o


def triplet_up(f0: float, f1: float, f2: float, bpm: float = 120) -> list[float]:
    return concat(note(f0, 0.35, bpm, 0.68), note(f1, 0.35, bpm, 0.68), note(f2, 0.45, bpm, 0.74))


def triplet_down(f0: float, f1: float, f2: float, bpm: float = 120) -> list[float]:
    return concat(note(f0, 0.4, bpm, 0.5), note(f1, 0.4, bpm, 0.48), note(f2, 0.55, bpm, 0.45))


# "Twinkle"-style — kids recognize it; spaced notes, not a wall of sound
WARMUP = [
    (C5, 1.0), (C5, 1.0), (G5, 1.0), (G5, 1.0),
    (A5, 1.0), (A5, 1.0), (G5, 2.0),
    (E5, 1.0), (E5, 1.0), (D5, 1.0), (D5, 1.0),
    (C5, 1.0), (C5, 1.0), (G4, 2.0),
]
WARMUP_BASS = [(C3, 8.0), (G3, 8.0)]

PLAY = [
    (E5, 0.5), (G5, 0.5), (C6, 0.5), (G5, 0.5),
    (E5, 0.5), (C5, 0.5), (G5, 1.0),
    (D5, 0.5), (G5, 0.5), (B5, 0.5), (G5, 0.5),
    (E5, 0.5), (C5, 0.5), (G5, 1.0),
]
PLAY_BASS = [(C3, 4.0), (G3, 4.0)]

RESULT = [
    (C5, 0.4), (E5, 0.4), (G5, 0.4), (C6, 0.8),
    (G5, 0.4), (E5, 0.4), (C5, 1.2),
]
RESULT_BASS = [(C3, 2.0), (G3, 2.0)]


def main() -> None:
    # Faster, fuller (melody + soft bass pad) — more upbeat/cheerful than the plain single-voice loops.
    warmup = melody_with_bass(110, WARMUP, 2, WARMUP_BASS)
    play = melody_with_bass(124, PLAY, 3, PLAY_BASS)
    result = melody_with_bass(128, RESULT, 1, RESULT_BASS)

    mapping = {
        # Countdown dùng cùng nhạc Prepare — đỡ đổi track liên tục
        "music_prepare.wav": warmup,
        "music_countdown.wav": warmup,
        "music_gameplay.wav": play,
        "music_result.wav": result,
        "sfx_button_click.wav": note(C5, 0.25, 120, 0.72),
        "sfx_button_hover.wav": note(E5, 0.15, 120, 0.42),
        "sfx_screen_whoosh.wav": note(G4, 0.3, 100, 0.38),
        "sfx_aruco_all_ready.wav": triplet_up(G4, C5, E5, 116),
        "sfx_countdown_tick.wav": note(A5, 0.2, 124, 0.55),
        "sfx_countdown_wait_pulse.wav": note(E5, 0.25, 100, 0.35),
        "sfx_countdown_go.wav": triplet_up(C5, E5, G5, 120),
        "sfx_question_new.wav": note(G5, 0.3, 116, 0.48),
        "sfx_mat_ready.wav": triplet_up(E5, G5, C6, 116),
        "sfx_timer_tick.wav": note(C6, 0.12, 120, 0.28),
        "sfx_timer_tick_warning.wav": note(A5, 0.18, 120, 0.42),
        "sfx_timer_urgent_loop.wav": note(E5, 0.15, 120, 0.25),  # unused if loop disabled
        "sfx_cover_select_start.wav": note(D5, 0.28, 112, 0.45),
        "sfx_cover_charge_mid.wav": note(E5, 0.15, 120, 0.32),
        "sfx_cover_lock_in.wav": note(G5, 0.2, 120, 0.38),
        "sfx_answer_correct.wav": triplet_up(C5, E5, G5, 124) + note(C6, 0.5, 108, 0.68),
        "sfx_answer_wrong.wav": triplet_down(E5, D5, C5, 104),
        "sfx_timeout.wav": triplet_down(G4, E4, C4, 98),
        "sfx_podium_correct.wav": note(G5, 0.25, 124, 0.4),
        "sfx_podium_wrong.wav": note(D4, 0.25, 120, 0.32),
        "sfx_result_fanfare.wav": triplet_up(C5, E5, G5, 120) + note(C6, 0.6, 108, 0.62),
        "sfx_star_pop.wav": note(C6, 0.22, 124, 0.58),
        "sfx_countdown_pop_anim.wav": note(A5, 0.12, 124, 0.4),
        "sfx_feedback_panel_in.wav": note(C5, 0.2, 114, 0.35),
    }

    for name, samples in mapping.items():
        write_wav(OUT / name, samples)
        print("wrote", OUT / name)


if __name__ == "__main__":
    main()
