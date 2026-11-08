# Audio Remastering for a Solo Developer

**Date**: 2026-11-08  
**Context**: Ticket #158 / Spec #152. Research only; production remastering is out of scope.  
**Source assets**: 11 music tracks and 22 effects extracted from the frozen `6_all.AllData_sound.bin` (33 named clips, 8.3 MB total). The source is a 2011 Flash game bank; the music is mixed and lossy, and no multitrack sessions or MIDI files survived.

---

## 1. What "remaster" can mean here

The word covers five very different projects. They should not be confused when estimating:

| Option | What it changes | Fidelity risk | Typical effort |
|---|---|---|---|
| **A. Preserve original** | Nothing; play the 2011 MP3s through the mixer | None | Already delivered |
| **B. Light mastering / cleanup** | Loudness, de-click, de-hum, gentle EQ, trims, mono-compatibility | Low–medium; can flatten the original character | 1–3 days for 33 clips |
| **C. Stem separation and remix** | AI-split music into stems, rebalance, add width/compression | Medium–high; separation artifacts on a lossy mix | 3–10 days |
| **D. Re-record / re-orchestrate** | New performances or sample libraries from the original compositions | High; requires the composition rights and a new creative direction | Weeks–months |
| **E. Full modern remake** | New music and SFX design, not a remaster | Total replacement | Months |

The important constraint: **neither stems nor original project files exist**. Any option above B is reconstruction, not restoration. AI separation can isolate parts of a stereo mix, but it cannot recover detail that the 2011 MP3 encoder and mix already discarded.

---

## 2. Option analysis

### A. Preserve the original (recommended baseline)

- **Effort**: zero beyond the extraction and routing already delivered.
- **Cost**: zero.
- **Strengths**: exact preservation intent, no rights ambiguity beyond the existing fan-project disclaimer, no new asset review, no gameplay retuning. The mixer can still provide modern loudness consistency with the Music/SFX/UI groups.
- **Weaknesses**: the source is a 2011 lossy bank; some tracks may have limited high-frequency detail, and the built-in loops are short.

This is the only option that is automatically faithful. It is also the only option that can be shipped without a separate listening/approval pass.

### B. Light mastering / cleanup

- **Work**: batch de-click/de-hum, trim leading/trailing silence, gentle EQ, optional loudness normalisation, loop-point review. Keep the original stereo image and arrangement.
- **Effort**: roughly 1–3 days for 33 clips, plus a blind A/B pass with the owner. Music needs more care than short effects.
- **Cost**: $0 with Audacity + FFmpeg. iZotope RX 12 Standard is $399 list; RX 12 Advanced is $1,399 list; iZotope Photon (local GPU stem separation and cleanup) is $349 list. Reaper is $60 for a discounted individual licence or $225 commercial.
- **Risks**: normalising every clip to the same LUFS can destroy the relative balance the original mixer relied on. A light pass should preserve per-clip gain and only remove obvious defects.
- **Verdict**: reasonable as an optional "remastered audio" pack, but not as the default. It changes the preservation target and needs a listening gate.

### C. AI stem separation and remix

- **Work**: separate music into vocals/drums/bass/other, rebalance, add modern compression/width, then remix. For this game the music is largely instrumental; the value is mostly in rebalancing, not in rescuing vocals.
- **Effort**: 3–10 days, mostly listening and fixing separation artifacts. Each of the 11 tracks needs individual attention.
- **Cost**: $0 with Demucs (open source), or $349 for iZotope Photon, or subscription credit on hosted services. The real cost is the solo developer's time.
- **Risks**: separation smears cymbals and transients; a 2011 MP3 has little headroom to hide artifacts. The result may sound "modern" but less like the original. It also creates a second canonical soundtrack that has to be maintained.
- **Verdict**: not worth it for a preservation-first fan project. Only justified if the goal changes to a commercial-style remaster with a new mix identity.

### D. Re-record / re-orchestrate

- **Work**: recover or transcribe the compositions, hire players or use sample libraries, record, mix, and master 11 tracks, then redo SFX design.
- **Effort**: weeks to months. The original compositions are short loop-based cues, but producing 35–45 minutes of finished music is still a full EP-sized project.
- **Cost**: indicative indie commissioning is roughly $200–$1,000 per finished minute, so **$7,000–$45,000** for 35–45 minutes before revisions and rights work. This is an estimate, not a quote.
- **Risks**: composition rights are unclear; the result stops being "the original audio"; every gameplay mix decision has to be re-approved.
- **Verdict**: out of scope for a solo preservation port.

### E. Full modern remake

- **Work**: new music, new SFX, new mix, new implementation.
- **Effort**: months.
- **Cost**: five figures or more.
- **Verdict**: a different product, not this ticket.

---

## 3. Tooling and cost summary

Indicative USD list prices observed on 2026-11-08; sales and regional pricing vary.

| Tool | Role | Cost |
|---|---|---:|
| FFmpeg | Batch conversion, loudness analysis (`loudnorm`), format checks | Free |
| Audacity | De-click, trim, EQ, spectral repair for small jobs | Free |
| DaVinci Resolve (Fairlight) | Free DAW/editor with restoration tools | Free |
| Reaper | Lightweight DAW for batch cleanup and effects | $60 discounted / $225 commercial |
| iZotope RX 12 Elements | Basic repair and de-click | ~$99 list |
| iZotope RX 12 Standard | De-click, de-hum, spectral repair, loudness | $399 |
| iZotope RX 12 Advanced | Full restoration suite, stem view | $1,399 |
| iZotope Ozone 12 Standard | Mastering chain, loudness targeting | $219 |
| iZotope Photon | Local GPU stem separation and cleanup | $349 |
| Demucs | Open-source music stem separation | Free |
| Adobe Audition | Subscription editor | ~$23/month |

A practical solo budget for option B is **$0–$100**: Audacity/Reaper plus FFmpeg covers de-click, trim, EQ and loudness checks. RX Standard only makes sense if several source files have serious artefacts.

---

## 4. Loudness and engine-side considerations

- Unity's mixer already provides Music, SFX and UI groups. Prefer fixing mix balance in the mixer or `AudioSource.volume` before baking changes into clips.
- If a remastered pack is produced, use EBU R128 / ITU-R BS.1770 measurement and document the target. A common game target is around **-16 LUFS integrated** for music, with short effects left at their original relative gain; this is a starting point, not a universal rule.
- Keep the original clips as the canonical bank. A remaster should be an alternate asset set selected by a setting, not an in-place overwrite.
- Music clips are imported as **Streaming** in Unity; effects are **Decompress On Load**. A remastered pack should preserve those import rules.
- Always A/B against the original on the same output device and at the same perceived loudness. Loudness differences are easily mistaken for "clarity".

---

## 5. Recommendation

**Ship option A now: preserve the original bank and route it through the Music/SFX/UI mixer.** This is already implemented and is the right default for a preservation project. It has no rights, cost or fidelity risk, and it keeps the door open for an optional remaster later.

If a remaster is revisited, do **option B only**, as a separately selectable asset pack:

1. Budget 1–3 days and $0–$100.
2. Process only obvious defects (clicks, DC offset, excessive silence, mono compatibility).
3. Preserve per-clip gain; do not loudness-normalise the whole bank to one number.
4. Run an owner A/B check on the hangar music and one combat track before accepting it.
5. Keep the original clips and the original trigger table untouched.

Do not start option C, D or E unless the project goal changes from preservation to a commercial remake. The source is a mixed, lossy 2011 bank; stem separation and re-recording replace the artefact rather than restore it, and the cost is measured in weeks or five figures.

---

## 6. References

- JPEXS FFDec (sound bank extraction): <https://github.com/jindrapetrik/jpexs-decompiler>
- Unity Audio Mixer manual: <https://docs.unity3d.com/6000.0/Documentation/Manual/AudioMixer.html>
- Unity audio import settings: <https://docs.unity3d.com/6000.0/Documentation/Manual/class-AudioClip.html>
- FFmpeg loudnorm filter: <https://ffmpeg.org/ffmpeg-filters.html#loudnorm>
- EBU R128 loudness recommendation: <https://tech.ebu.ch/publications/r128>
- iZotope RX 12 Standard, $399: <https://www.izotope.com/products/rx-standard>
- iZotope RX 12 Advanced, $1,399: <https://www.izotope.com/products/rx-advanced>
- iZotope Photon, $349: <https://www.izotope.com/products/photon>
- iZotope Ozone 12 Standard, $219: <https://www.izotope.com/products/ozone-standard>
- Reaper licence, $60/$225: <https://www.reaper.fm/purchase.php>
- Demucs source separation: <https://github.com/facebookresearch/demucs>
- Audacity: <https://www.audacityteam.org/>
- DaVinci Resolve: <https://www.blackmagicdesign.com/products/davinciresolve>
