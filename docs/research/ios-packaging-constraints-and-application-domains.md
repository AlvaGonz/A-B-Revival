# Research: iOS Packaging Constraints (IPA, Application Domains & Code Loading)

**Date:** 2026-11-07  
**Parent Ticket:** #56 (Spec: AIR port of A&B to Android)  
**Ticket:** #65 (Research: iOS packaging constraints)  
**Status:** Complete  

---

## 1. Executive Summary

Packaging *Assembly & Battle* for Apple iOS via Adobe AIR (`.ipa`) introduces strict technical and platform constraints fundamentally different from Windows desktop or Android. 

The two primary gates are:
1. **Apple `W^X` Memory Execution Policy:** iOS forbids runtime JIT compilation and dynamic code execution. All ActionScript must be compiled Ahead-Of-Time (AOT) to native ARM64 machine instructions.
2. **Sub-SWF Asset Loading Seam (`Fly4UI.as`):** The current desktop loader uses `Loader.loadBytes(..., allowCodeImport = true)` to load `data.bin`, `sound.bin`, and `ui.bin`. On iOS, `allowCodeImport = true` raises fatal runtime `SecurityError #3769`.

### Recommendation
- **Android Port (Milestone 1):** **GO (Immediate)**. Android supports captive runtime packaging (`.apk` / `.aab`), accepts `allowCodeImport = true`, and requires no Apple provisioning friction.
- **iOS Port (Milestone 2):** **CONDITIONAL NO-GO for Proof-of-Concept; GATED to Post-Android Release**. iOS requires refactoring the sub-SWF asset loader and obtaining Apple Developer signing credentials.

---

## 2. Technical Deep-Dive

### 2.1 The `allowCodeImport` Fatal Constraint on iOS

In `src/as3/scripts/Fly4UI.as` (lines 141–167):
```actionscript
var _ctx1:LoaderContext = new LoaderContext();
_ctx1.allowCodeImport = true;
_loc1_.loadBytes(new this.alldata.data(), _ctx1);
```

#### Why it fails on iOS:
Under Apple App Store Review Guidelines (Guideline 2.5.2) and iOS kernel constraints:
- Applications may not download, install, or execute code that introduces new features or functionality.
- In Adobe AIR AOT mode (`-target ipa-app-store` / `ipa-ad-hoc`), the ADT packager translates ActionScript 3 bytecode into native ARM64 assembly at compile time.
- At runtime, `Loader.loadBytes()` cannot compile bytecode into executable memory because iOS marks data memory pages as Non-Executable (`NX` bit / `W^X`).
- Calling `loadBytes()` with `allowCodeImport = true` immediately throws:
  ```
  SecurityError: Error #3769: Security sandbox violation: Loader.loadBytes is not permitted with allowCodeImport=true
  ```

#### Solution for Milestone 2:
1. **Inspect Sub-SWF Contents:** The sub-SWFs (`data.bin`, `sound.bin`, `ui.bin`) contain vector graphics, MovieClips, and audio—not executable ActionScript classes.
2. **Strip Bytecode & Use Safe Context:**
   ```actionscript
   var _ctx:LoaderContext = new LoaderContext(false, ApplicationDomain.currentDomain);
   _ctx.allowCodeImport = false;
   _loc1_.loadBytes(bytes, _ctx);
   ```
   When `allowCodeImport = false`, the iOS AOT runtime permits loading vector shapes and movieclips into the current `ApplicationDomain` for instantiation via `Attach.attach()`.
3. **Alternative (Single-SWF Asset Merging):** Merge all symbol assets directly into the primary SWF during packaging, bypassing `loadBytes` entirely.

---

## 3. iOS Provisioning & Packaging Requirements

To package and deploy an iOS `.ipa`:
1. **Apple Developer Account:** Active membership (\$99/year).
2. **Signing Assets Required by ADT:**
   - Apple Worldwide Developer Relations (WWDR) Intermediate Certificate.
   - iOS Distribution Certificate (`.p12` file with private key).
   - App Store or Ad-Hoc Provisioning Profile (`.mobileprovision`).
3. **ADT Packaging Command:**
   ```bash
   adt -package -target ipa-app-store \
       -provisioning-profile app.mobileprovision \
       -keystore ios_distribution.p12 -storetype pkcs12 \
       dist/AssemblyAndBattle.ipa \
       src/air/application.xml \
       game/a&b_space_mod.swf \
       assets/
   ```
4. **App Store Compliance:**
   - Universal binary (iPhone & iPad).
   - Launch storyboard or high-resolution launch images.
   - Privacy manifest (`PrivacyInfo.xcprivacy`).

---

## 4. Comparison: Android vs. iOS Deployment

| Feature | Android (Milestone 1) | iOS (Milestone 2) |
|---|---|---|
| **Compilation Mode** | Captive JIT / Native | Strict AOT (ARM64) |
| **`allowCodeImport = true`** | Allowed | **Blocked (Throws #3769)** |
| **Packaging Prerequisite** | Free self-signed keystore | \$99/yr Apple Dev Profile + Cert |
| **Testing Friction** | Direct `adb install` | TestFlight / Ad-Hoc UDID registration |
| **Effort Estimate** | 10–15 hours | 35–50 hours |
| **Recommendation** | **PROCEED (GO)** | **DEFER (GATED)** |

---

## 5. Decision & Next Steps

1. **Gate iOS Deployment:** Formally record that the iOS deployment target is deferred until Android PoC is validated.
2. **Refactor Loader Seam:** In preparation for future iOS compatibility, update `Fly4UI.as` to test `allowCodeImport = false` with `ApplicationDomain.currentDomain`.
