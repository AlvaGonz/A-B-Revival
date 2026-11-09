# Game-Dev Skills Inventory: External Repos vs A&B Revival

**Date:** 2026-11-09  
**Ticket:** #188 (Research: inventory external Unity game-dev skills for project-local adoption)  
**Parent:** Wayfinder Map #122  
**Status:** Complete — research only; nothing installed, copied or executed  
**Related:** ADR 0014 (Unity 6.6 DOTS), ADR 0015 (AS3 frozen), ADR 0016 (two-tier tests), ADR 0020 (asset fidelity), ticket #124 (sprite atlas), ticket #145 (E2E testing)

---

## 1. Executive summary

Three public repositories were cloned at pinned commits into a scratch directory outside this repository and read in full. No skill was installed, no external script was executed, and no account, key or subscription was recorded.

| Measure | Count |
|---|---|
| Repositories | 3 (all public, MIT / Apache-2.0 / MIT) |
| `SKILL.md` files | **160** (game-ui 8, awesome 78 incl. router, CCGS 74) |
| Claude Code subagents | **49** |
| README "What you can ask" rows | **8** |
| Manifest records | **217** (160 skills + 49 agents + 8 ask rows) |
| Recommendation: adopt | **4** |
| Recommendation: adapt | **17** |
| Recommendation: reject | **196** |

**Top findings**

1. **One path serves both harnesses.** OpenCode documents `.opencode/skills/` as its canonical project directory and also reads `.claude/skills/` and `.agents/skills/` for compatibility. Antigravity's App, CLI and IDE all default to `.agents/skills/` and still read legacy `.agent/skills/`. Therefore **`.agents/skills/` is the only project-local skill directory both harnesses read natively**; `.opencode/skills/` is OpenCode-only and `.claude/skills/` is not read by Antigravity.
2. **`awesome-gamedev-agent-skills` is the best source.** Its 78 skills use only the portable Agent Skills core (`name` + `description`), it is Apache-2.0 with a NOTICE, it has a validator and tests, and it explicitly tells agents to keep the project's pinned Unity version rather than upgrade to its 6.3 LTS baseline.
3. **`Claude-Code-Game-Studios` is a studio workflow, not a skill pack.** 67 of its 74 skills embed `!` bash injection, all 74 declare Claude Code `allowed-tools`, and most depend on `project.yaml`, a `production/` tree and `.claude/scripts`. Three Unity agents and two test skills are worth porting; the rest duplicate or contradict the wayfinder/to-spec/to-tickets/TDD flow.
4. **`game-ui-design-workflow` is rejected wholesale.** Its pipeline generates page and component artwork, which directly conflicts with ADR 0020 (original Flash art, fonts and layout). Its sprite splitting and atlas work duplicate `scripts/export-*.bat` and ticket #124. Its skill bodies are Simplified Chinese, adding translation cost on top of the conflict.
5. **Image generation is never bundled.** Only `game-ui-page-generator` / `game-ui-component-breakdown` and the optional path in `create-game-assets` produce images, and each needs an external image-generation tool or account. No live credential was found in any of the three repositories; the only key-like strings are placeholder `sk_live_...` examples inside a CCGS security-audit test fixture, not secrets.

**Recommended phase-1 install (4 skills, all adopt-as-is, Apache-2.0):** `game-ui-ux`, `performance-optimization`, `unity-input-system`, `unity-scriptableobjects`.

**Recommended phase-2 ports:** `physics-tuning`; the `unity-ui-specialist`, `unity-dots-specialist` and `accessibility-specialist` agents; `create-game-assets` (non-generation half only); `game-ai`, `game-feel`, `camera-systems`, `unity-csharp-scripting`, `unity-build-pipeline`; `test-helpers`.

---

## 2. Method, sources and verified discovery paths

**Method.** Each repository was cloned with `git clone --depth 1`, pinned to the commit below, and inspected: every `SKILL.md` frontmatter and body, every agent file, the README catalog, install/compatibility docs, licenses, NOTICE files, scripts, schemas and templates. No external code was run. The inventory was generated from the actual files, not from the README counts.

| Repo | Commit | Commit date | License | Stars | Last push | Release |
|---|---|---|---|---|---|---|
| `guiguiyan930-source/game-ui-design-workflow` | `1ec6046` | 2026-08-05 | MIT | 415 | 2026-08-05 | v0.3.1 |
| `gamedev-skills/awesome-gamedev-agent-skills` | `19d93f9` | 2026-10-28 | Apache-2.0 (+ NOTICE) | 7,945 | 2026-10-28 | v1.1.0 |
| `Donchitos/Claude-Code-Game-Studios` | `05ad7c8` | 2026-10-19 | MIT | 37,295 | 2026-10-19 | v1.2.0 |

**Repo access issues:** none. All three repositories are public and cloned successfully. No private, archived or missing repository was encountered.

### 2.1 Verified project-local paths (checked 2026-11-09)

| Harness | Project skills | Global skills | Project agents | Trigger |
|---|---|---|---|---|
| **OpenCode** | `.opencode/skills/` (canonical); also `.claude/skills/`, `.agents/skills/` (compatibility) | `~/.config/opencode/skills` | `.opencode/agents/<name>.md` | auto by description; explicit `@skill-id` |
| **Antigravity** | `.agents/skills/<skill-folder>/` (default); `.agent/skills/` (legacy) | `~/.gemini/config/skills` (App/CLI/IDE); CLI also `~/.gemini/antigravity-cli/skills`, `~/.gemini/skills` | `.agents/agents/<name>.md` or `.agents/agents/<name>/agent.md` | auto by description; `/skill-name` |

Official sources:

- OpenCode skills: <https://opencode.ai/docs/skills> — documents project `.opencode/skills`, compatibility `.claude/skills` and `.agents/skills`, nested `SKILL.md`, path-derived IDs, precedence and permissions.
- OpenCode agents: <https://opencode.ai/docs/agents> — documents `.opencode/agents/<name>.md`, `mode: subagent`, model/permission fields.
- Antigravity skills: <https://antigravity.google/docs/skills/> — states "Antigravity defaults to `.agents/skills`, but still maintains backward compatibility for `.agent/skills`" and lists the global path.
- Antigravity subagents: <https://antigravity.google/docs/subagents/> — documents `.agents/agents/<name>.md` and the frontmatter fields.

**Consequences for this proposal**

- A single flat tree at **`.agents/skills/<skill-id>/`** is discovered by both harnesses with no configuration and no duplication.
- **Do not use `.opencode/skills/` as the canonical store**: Antigravity cannot read it, so a second copy or symlink would be required and would drift.
- **Nested vendor folders** such as `.agents/skills/awesome/unity-input-system/` are explicitly supported by OpenCode (up to six levels) but Antigravity's docs only promise `<skill-folder>` direct children. Until a live Antigravity test proves nesting works, keep skill folders flat.
- **Agents cannot be shared.** OpenCode and Antigravity use different frontmatter (`description`/`mode`/`permissions` vs `name`/`tools`/`subagent`/`commandExecutionPolicy`). Any adopted CCGS agent needs two files with the same body and different frontmatter; this is a phase-2 decision, not part of the skill install.

---

## 3. Repository profiles

### 3.1 `game-ui-design-workflow` — MIT, 415 stars, last push 2026-08-05

**What it is.** A Cursor-oriented, Chinese-language workflow that turns a game concept into GDD/PRD/spec-kit documents, generates UI page images, breaks pages into components, splits sprite sheets, packs atlases, and exports engine manifests for Godot, Unity, Cocos or generic consumers.

**Contents.** 8 skills, 6 Python scripts, 2 JSON schemas, 7 contract templates, 8 spec-kit document templates, 3 complete worked projects plus 4 focused examples, 2 config YAMLs, and a POSIX `install.sh` that copies skills into `.cursor/skills`.

**Dependencies.** Python 3.9+ with PyYAML and Pillow for the scripts; an external image-generation tool/account for page and component images; POSIX shell (Windows requires WSL or Git Bash). No MCP server, API key or vendor SDK is bundled.

**Format.** `SKILL.md` frontmatter is only `name` + `description`, so the files load in OpenCode and Antigravity. The bodies are Simplified Chinese and the installer targets Cursor only; using it here means translation plus manual vendoring.

**Maintenance.** Created and pushed in a single one-day burst (2026-08-04/05), v0.3.1, one open issue. Low bus factor and no evidence of a second release cycle. Treat as a snapshot, not a maintained dependency.

**Verdict:** reject. The generated-art pipeline conflicts with ADR 0020, the extraction/atlas work duplicates ticket #124 and `scripts/export-*.bat`, and the document flow conflicts with the issue-tracker-only rule in `AGENTS.md`.

**Linked sibling repositories (not cloned).** The README links `Game-UI-Extension`, `game-ui-page-generator`, `game-ui-specification` and `game-ui-component-breakdown`. They are the standalone upstreams of the same eight skills, so treating them as separate sources would duplicate the same content; revisit only if one of their skill bodies diverges from this bundle.

### 3.2 `awesome-gamedev-agent-skills` — Apache-2.0, 7,945 stars, last push 2026-10-28

**What it is.** 77 engine/discipline/genre/workflow skills plus a router, across 12 engines. Every skill is a plain Agent Skills folder (`name` + `description`, optional `references/`, `scripts/`, `assets/`), validated by `scripts/validate-skills.py` and covered by 10 regression tests.

**Contents by group.** 49 engine skills (Godot 16, Unity 8, Unreal 6, Ebitengine 1, web 6, other engines 12), 15 cross-engine disciplines, 9 genre templates, 4 workflows, 1 router. `docs/VERSION-SUPPORT.md` pins Unity 6.3 LTS (6000.3) as the *new-project baseline* and explicitly says existing projects keep the version in `ProjectSettings/ProjectVersion.txt`; this repository is Unity 6.6 (6000.6.8f1), so version-aware behaviour is handled by the skill instructions rather than by editing the skills.

**Dependencies.** Markdown only for almost everything. `create-game-assets` optionally uses Python 3.10+ and Pillow 10-13 for its QA scripts and an external image tool for generation. `steam-publish` needs steamcmd/Steamworks, `itch-publish` needs butler/itch.io, `godot-export`/`godot-gdscript-headless-testing` need the Godot CLI — all rejected here.

**Format.** The cleanest of the three: only the two portable frontmatter fields, no Claude Code extensions, no command injection. Native in OpenCode and Antigravity.

**Maintenance.** Active; 7,945 stars, 778 forks, v1.1.0, last push 2026-10-28, a validator and a stated quarterly baseline review.

**Verdict:** the primary source. 4 adopt, 10 adapt, 64 skill rejects (mostly other engines/genres).

### 3.3 `Claude-Code-Game-Studios` — MIT, 37,295 stars, last push 2026-10-19

**What it is.** A Claude Code "game studio": 49 subagents in a director/lead/specialist hierarchy, 74 slash-command skills, 18 hook scripts (README advertises 15 hooks), 14 path-scoped rules, 71 files under `.claude/docs/templates` (62 advertised document templates plus guidance), 35 scripts, 25 docs, 46 engine-reference files, and 74 skill-test specs plus 49 agent-test specs under `studio-tests/`.

**Harness coupling.** All 74 skills declare `allowed-tools`; 67 embed `!` bash injection; 68 reference `${CLAUDE_SKILL_DIR}`; 5 disable model invocation. The bodies assume `project.yaml`, a `production/` tree, `.claude/scripts` and hooks. Agents use Claude Code subagent frontmatter (`tools`, `model: opus|sonnet|inherit`, `maxTurns`). None of that maps natively: the documented OpenCode and Antigravity skill schemas do not include `allowed-tools` or `!` command execution, so those behaviours are not available and a straight copy would read like instructions while silently skipping its setup and validation.

**Engine coverage.** Five Godot agents, five Unreal/UE agents and five Unity agents. The Unity reference tree is pinned to Unity 6.3 LTS and its `unity-dots-specialist` assumes Entities/ECS (`IComponentData`, `EntityQuery`, baking) while this project has **no `com.unity.entities` package** — it uses raw `NativeArray`, `IJobParallelFor` and `[BurstCompile]` directly (ADR 0014; `A&B-U/Packages/packages-lock.json`).

**Maintenance.** Very active; 37,295 stars, 4,901 forks, 96 open issues, v1.2.0 on 2026-10-19. The cost is surface area and harness lock-in, not staleness.

**Verdict:** 3 agents and 2 skills are worth porting; 118 records rejected because they duplicate the project's process, assume a studio hierarchy/GDD tree, or are Godot/Unreal-only.

---

## 4. Full inventory

The tables below cover every `SKILL.md`, every agent and every README ask row. Verdicts are `adopt` (vendor as-is), `adapt` (vendor with edits) or `reject` (do not vendor). Priority is P1 (do first), P2 (next) or P3 (deferred). License, maintenance and dependency fields are per record in `docs/research/game-dev-skills-manifest.json`; maintenance is repo-level (section 3). Harness support is uniform per source: game-ui and awesome are `native` in both OpenCode and Antigravity, CCGS is `port` in both.

| Repo | Skills | Adopt | Adapt | Reject |
|---|---:|---:|---:|---:|
| `game-ui-design-workflow` | 8 | 0 | 0 | 8 |
| `awesome-gamedev-agent-skills` | 78 | 4 | 10 | 64 |
| `Claude-Code-Game-Studios` | 74 | 0 | 2 | 72 |
| **Skills subtotal** | **160** | **4** | **12** | **144** |
| CCGS agents | 49 | 0 | 3 | 46 |
| Awesome ask rows | 8 | 0 | 2 | 6 |
| **All records** | **217** | **4** | **17** | **196** |

### 4.1 `game-ui-design-workflow` — 8 skills

| Skill | Purpose | Engine | Category | Verdict |
|---|---|---|---|---|
| game-ui-asset-pipeline | Packs semantically named transparent game UI PNG files into an atlas, preserves component states and reviewed 9-slice... | agnostic (Unity/Godot/Cocos JSON) | art | reject |
| game-ui-component-breakdown | Breaks an approved game UI page or visual style into reusable, individually generated component assets with clean edges,... | agnostic | art | reject |
| game-ui-extension | Extends a game concept or partial interface into a coherent screen system, player journey, feature flow, prioritized screen... | agnostic | ui | reject |
| game-ui-page-generator | Designs and generates one complete game UI page at a time using an approved style and screen contract, producing a page... | agnostic | art | reject |
| game-ui-product-design | Writes and maintains game design docs (GDD), product requirements (PRD), and UI interaction logic before any page image... | agnostic | docs | reject |
| game-ui-specification | Creates and maintains production-oriented game UI specifications and shared visual contracts covering color, typography,... | agnostic | ui | reject |
| game-ui-sprite-sheet-splitter | Splits a generated game UI sprite sheet or component board into individual transparent PNG elements, writes crop metadata,... | agnostic | art | reject |
| game-ui-workflow | Orchestrates an end-to-end game UI workflow from game design (GDD), PRD, and UI interaction logic through design... | agnostic (Unity/Godot/Cocos JSON) | ui | reject |

**Support assets (not skills).** `install.sh` (Cursor-only), `scripts/init_project.py`, `validate_project.py`, `validate_skills.py`, `split_sprite_sheet.py`, `build_sprite_atlas.py`, `export_engine_manifest.py`; `schemas/component.schema.json`, `ui-token.schema.json`; `templates/contracts/` (7 YAML contracts), `templates/spec-kit/` (8 documents); `examples/` (3 complete UI projects); `config/slice-rules.yaml`, `export-params.yaml`.

### 4.2 `awesome-gamedev-agent-skills` — router and Unity skills

| Skill | Purpose | Engine | Category | Verdict |
|---|---|---|---|---|
| router | Routes any game-development request to the right specialized skill(s): it detects the engine (Godot, Unity, Unreal,... | multi | workflow | reject |
| unity-animation | Drive Unity 6.3 LTS character animation with Animator Controllers: states, transitions, parameters, blend trees, animation... | Unity 6.3+ | gameplay | reject |
| unity-build-pipeline | Build and ship Unity 6.3 LTS players: build settings and scenes, player/quality settings, the IL2CPP vs Mono scripting... | Unity 6.3+ | production | **adapt** (P2) |
| unity-csharp-scripting | Write Unity 6.3 LTS C# gameplay scripts: the MonoBehaviour lifecycle (Awake/OnEnable/Start/Update/FixedUpdate/LateUpdate),... | Unity 6.3+ | gameplay | **adapt** (P2) |
| unity-input-system | Wire player input in Unity 6.3 LTS with the Input System package: Input Actions, action maps, the PlayerInput component,... | Unity 6.3+ | gameplay | **adopt** (P1) |
| unity-navmesh | Add AI navigation in Unity 6.3 LTS: bake a NavMesh with the AI Navigation package (NavMeshSurface), move agents with... | Unity 6.3+ | ai | reject |
| unity-physics | Set up 3D physics in Unity 6.3 LTS: Rigidbody movement and forces, colliders, triggers vs collisions, layer-based... | Unity 6.3+ | gameplay | reject |
| unity-scriptableobjects | Architect Unity 6.3 LTS data and decoupling with ScriptableObjects: config/data assets, shared runtime variables, event... | Unity 6.3+ | gameplay | **adopt** (P1) |
| unity-tilemap-2d | Build and script 2D tilemaps in Unity 6.3 LTS: the Grid + Tilemap components, the Tile Palette, tilemap colliders, rule... | Unity 6.3+ | gameplay | reject |

> The router is rejected: OpenCode/Antigravity already select skills by description, and the router would happily route a Unity repository to Godot/Unreal skills.

### 4.3 `awesome-gamedev-agent-skills` — cross-engine disciplines (15)

| Skill | Purpose | Engine | Category | Verdict |
|---|---|---|---|---|
| ai-behavior-trees-utility-ai | Build a production behavior-tree runtime (Blackboard, action/condition leaves, sequence/selector/parallel composites,... | agnostic | ai | reject |
| audio-design | Implement game audio practice — bus/mixer architecture and gain in decibels, ducking (sidechain), adaptive/dynamic music... | agnostic | audio | **adapt** (P3) |
| camera-systems | Build game cameras that feel good — 2D follow with a deadzone, look-ahead, smoothing, and level-bounds clamping; 3D... | agnostic | gameplay | **adapt** (P2) |
| create-game-assets | Plan, generate, source, normalize, and validate cohesive visual game assets. Use for art direction, style bibles, sprites,... | agnostic | art | **adapt** (P2) |
| dialogue-systems | Build branching dialogue and narrative — a node/choice graph with conditions, variables, and localization hooks — and... | agnostic | narrative | reject |
| game-ai | Design NPC and enemy decision-making with finite state machines, behavior trees, steering behaviors, and A* pathfinding —... | agnostic | ai | **adapt** (P2) |
| game-feel | Add "juice" and game feel that makes actions satisfying — screen shake, hit-stop/freeze frames, tweened/eased motion,... | agnostic | gameplay | **adapt** (P2) |
| game-ui-ux | Design and build game UI/UX — HUDs, menus, and overlays — that survive every screen: anchor- based responsive layout,... | agnostic | ui | **adopt** (P1) |
| input-systems | Architect game input — action mapping (abstracting keys into named actions), rebinding with conflict detection and... | agnostic | gameplay | reject |
| level-design | Design and build playable levels — the blockout/whitebox-to-playable workflow, player metrics and grid layout, pacing and... | agnostic | gameplay | reject |
| performance-optimization | Find and fix game performance problems methodically — measure with the engine profiler first, reason about the frame-time... | agnostic | gameplay | **adopt** (P1) |
| physics-tuning | Tune game physics for stable, good-feeling motion — fixed vs variable timestep, render interpolation, mass/gravity/drag,... | agnostic | gameplay | **adapt** (P1) |
| procedural-gen | Generate game content procedurally — seeded deterministic RNG, value/Perlin/ Simplex noise for terrain and heightmaps, grid... | agnostic | gameplay | **adapt** (P3) |
| save-systems | Design save/load for game state — choosing what to serialize, file formats, save slots, atomic crash-safe writes, schema... | agnostic | gameplay | **adapt** (P3) |
| shader-programming | Write game shaders from cross-engine fundamentals — the vertex→fragment pipeline, coordinate spaces, UV math, and common... | agnostic | art | reject |

### 4.4 `awesome-gamedev-agent-skills` — genres and workflows (13)

| Skill | Purpose | Engine | Category | Verdict |
|---|---|---|---|---|
| card-game | Build a card game: card data, deck/hand/discard zones, draw/shuffle/reshuffle, a turn structure, costs, and effect... | agnostic | gameplay | reject |
| fps-shooter | Build a first-person shooter: move+mouse-look controller, hitscan or projectile shooting, weapons, health, and enemy AI.... | agnostic | gameplay | reject |
| platformer | Build a 2D platformer: run/jump control with coyote time, jump buffering, and variable jump height, wall slide and wall... | agnostic | gameplay | reject |
| puzzle | Build a puzzle game: grid/board state, move input, rule-based resolution (match-3 cascades, sokoban pushes, tile logic),... | agnostic | gameplay | reject |
| roguelike | Build a roguelike: turn-based grid movement, procedural dungeons, permadeath, field-of-view, and loot tables. Use for a... | agnostic | gameplay | reject |
| rpg | Build an RPG: stats and leveling, inventory and equipment, quests, branching dialogue, save/load, and combat. Use for an... | agnostic | gameplay | reject |
| survival-crafting | Build a survival-crafting game: resource gathering, inventory, crafting and a tech tree, needs (hunger/thirst/temperature),... | agnostic | gameplay | reject |
| tower-defense | Build a tower defense: enemies pathing along lanes, wave spawning, towers that auto-target and fire, an economy, and lives.... | agnostic | gameplay | reject |
| visual-novel | Build a visual novel: a branching script, character and background display, a text box with choices, save/load, backlog,... | agnostic | gameplay | reject |
| game-jam | Plan and ship a game under a jam deadline: lock scope to the clock, schedule the hours, cut features, and submit on time.... | agnostic | production | reject |
| itch-publish | Publish and update a game on itch.io: create the project page and upload builds with the butler CLI (butler push) to named... | agnostic | production | reject |
| prototype-fast | Build a playable prototype in about an hour to answer one question — is it fun? — with greybox primitives, a hard timebox,... | agnostic | production | reject |
| steam-publish | Publish or update a game on Steam with Steamworks and SteamPipe: configure depots and packages, upload builds with... | agnostic | production | reject |

### 4.5 `awesome-gamedev-agent-skills` - other engines (41)

| Skill | Purpose | Engine | Category | Verdict |
|---|---|---|---|---|
| godot-2d-movement | Implement 2D kinematic character movement in Godot 4.7 with CharacterBody2D and move_and_slide(): platformer run/jump with... | Godot 4.7 | engine | reject |
| godot-3d-essentials | Set up a Godot 4.7 3D scene: Node3D transforms, Camera3D, lighting (DirectionalLight3D/OmniLight3D), WorldEnvironment for... | Godot 4.7 | engine | reject |
| godot-animation | Animate in Godot 4.7 three ways: AnimationPlayer for keyframed clips (incl. Call Method tracks), AnimationTree with state... | Godot 4.7 | engine | reject |
| godot-audio | Play and mix audio in Godot 4.7: AudioStreamPlayer (2D/3D variants), audio buses with volume/mute and effects, music vs SFX... | Godot 4.7 | engine | reject |
| godot-csharp | Use C#/.NET in Godot 4.7: partial classes extending nodes, the PascalCase lifecycle (_Ready/_Process/_PhysicsProcess),... | Godot 4.7 | engine | reject |
| godot-export | Export and build a Godot 4.7 project for distribution: install export templates, define export presets... | Godot 4.7 | engine | reject |
| godot-gdscript | Write idiomatic GDScript for Godot 4.7: static typing, the node lifecycle (_ready/_process/_physics_process),... | Godot 4.7 | engine | reject |
| godot-gdscript-headless-testing | Run GDScript test suites from the command line with `godot --headless`, using a SceneTree/MainLoop runner script that exits... | Godot 4.7 | engine | reject |
| godot-multiplayer | Build networked games with Godot 4.7 high-level multiplayer: set up an ENetMultiplayerPeer server/client, define RPCs with... | Godot 4.7 | engine | reject |
| godot-nodes-scenes | Structure a Godot 4.7 project with the scene tree and node composition: build reusable scenes, instance PackedScenes at... | Godot 4.7 | engine | reject |
| godot-physics | Use Godot 4.7 physics bodies and detection in 2D and 3D: RigidBody, StaticBody, Area, and CharacterBody; collision layers... | Godot 4.7 | engine | reject |
| godot-resources | Design data-driven Godot 4.7 games with custom Resource classes: define typed data with class_name + @export, save/load... | Godot 4.7 | engine | reject |
| godot-shaders | Write Godot 4.7 shaders in the Godot Shading Language: canvas_item shaders for 2D and spatial shaders for 3D, with... | Godot 4.7 | engine | reject |
| godot-signals-groups | Build event-driven, decoupled Godot 4.7 gameplay with signals and node groups: declare and emit custom signals, connect... | Godot 4.7 | engine | reject |
| godot-tilemap | Build and edit tile-based 2D levels in Godot 4.7 with TileMapLayer and TileSet: paint layers, set up... | Godot 4.7 | engine | reject |
| godot-ui-control | Build Godot 4.7 user interfaces with Control nodes: anchors and offsets for responsive layout, Container nodes... | Godot 4.7 | engine | reject |
| unreal-behavior-trees | Build NPC AI in Unreal Engine 5 with Behavior Trees and Blackboards: composites (Selector/Sequence), tasks, decorators,... | Unreal 5.8 | engine | reject |
| unreal-blueprints | Build Unreal Engine 5 gameplay with Blueprint visual scripting: Blueprint Classes, the Event Graph and Construction Script,... | Unreal 5.8 | engine | reject |
| unreal-cpp-gameplay | Write Unreal Engine 5 C++ gameplay code: the UCLASS/UPROPERTY/UFUNCTION reflection macros, the Gameplay Framework... | Unreal 5.8 | engine | reject |
| unreal-enhanced-input | Set up player input in Unreal Engine 5 with Enhanced Input: Input Actions, Input Mapping Contexts, modifiers and triggers,... | Unreal 5.8 | engine | reject |
| unreal-niagara | Create and control VFX in Unreal Engine 5 with Niagara: systems and emitters, modules and the spawn/update stages, exposed... | Unreal 5.8 | engine | reject |
| unreal-packaging | Package and ship an Unreal Engine 5 project: the Platforms menu Package Project flow, build configurations (Development vs... | Unreal 5.8 | engine | reject |
| ebitengine-game | Create, improve, debug and package playable 2D games in Go with Ebitengine (github.com/hajimehoshi/ebiten/v2). Use for... | Ebitengine | engine | reject |
| phaser-arcade-physics | Use Phaser 4 Arcade Physics: enable the world, give sprites bodies, set velocity/acceleration/gravity, and resolve... | Phaser 4.2 | engine | reject |
| phaser-core | Set up and debug a Phaser 4 game: the Game config, the Scene lifecycle (init/preload/create/update), the asset loader,... | Phaser 4.2 | engine | reject |
| pixijs-rendering | Build a PixiJS v8 render layer: create the async Application, load textures with Assets, compose the scene graph with... | PixiJS 8 | engine | reject |
| threejs-gltf-loading | Load glTF/GLB models in three.js with GLTFLoader and play their skinned animations with AnimationMixer, including... | three.js r186 | engine | reject |
| threejs-materials-lighting | Light and shade a three.js scene: choose materials (MeshStandardMaterial PBR vs unlit MeshBasicMaterial), add... | three.js r186 | engine | reject |
| threejs-scene-setup | Stand up a three.js scene: import maps and the three/addons path, the Scene/PerspectiveCamera/WebGLRenderer trio, the... | three.js r186 | engine | reject |
| bevy-ecs | Structure a Bevy app around its Entity Component System: build the App with plugins, define Component/Resource types, write... | Bevy 0.20 | engine | reject |
| pygame-core | Structure a pygame (pygame-ce) game in Python: the init/event/update/draw loop, delta-time movement, Surface/Rect blitting,... | pygame-ce | engine | reject |
| love2d-core | Structure and debug a LÖVE (Love2D) game in Lua: the love.load/update/draw loop, delta-time movement, input, and screen... | LOVE 11.5 | engine | reject |
| defold-core | Build and debug the core of a Defold game in Lua: game.project settings, collections, game objects and components, script... | Defold 1.14 | engine | reject |
| roblox-characters | Build respawn-safe Roblox character systems around Players, CharacterAdded/CharacterRemoving, Humanoid, HumanoidRootPart,... | Roblox | engine | reject |
| roblox-datastores | Persist player data in Roblox with DataStoreService: GetDataStore, GetAsync/ SetAsync/UpdateAsync/IncrementAsync wrapped in... | Roblox | engine | reject |
| roblox-luau | Script a Roblox experience in Luau: get services, create and parent Instances, connect events, run server Scripts vs client... | Roblox | engine | reject |
| roblox-networking | Design and harden Roblox client/server networking with RemoteEvent, RemoteFunction, and UnreliableRemoteEvent; server... | Roblox | engine | reject |
| roblox-physics | Implement Roblox physical simulation and queries with assemblies, anchoring, constraints, collision groups,... | Roblox | engine | reject |
| roblox-simulator-economy | Design, balance, and implement the economy of a Roblox simulator, tycoon, clicker, or idle game in Luau: upgrade cost... | Roblox | engine | reject |
| roblox-studio-workflow | Modify living Roblox Studio projects safely: inspect Explorer and execution locations, preserve existing structure, reuse... | Roblox | engine | reject |
| roblox-ui | Build production Roblox interfaces with ScreenGui/PlayerGui lifecycle, responsive UDim2 layouts, safe insets, reusable... | Roblox | engine | reject |

**Support assets (not skills).** `router/SKILL.md` with three references (`engine-detection`, `routing-table`, `custom-engines`); `scripts/validate-skills.py`, `generate-site.py`, `check-site.py`; `templates/SKILL.template.md`; 10 regression tests; `.claude-plugin/marketplace.json`; 4 docs (`COMPATIBILITY`, `INSTALLATION`, `SKILL-FORMAT`, `VERSION-SUPPORT`).

### 4.6 `awesome-gamedev-agent-skills` - "What you can ask" rows (8)

Every entry in the README section, including the composed skills it loads.

| Ask (README row) | Loads | Engine | Verdict |
|---|---|---|---|
| ask: double jump (Godot) | godot-2d-movement + platformer | Godot 4.7 | reject |
| ask: Unity RPG inventory | unity-scriptableobjects + rpg + save-systems | Unity 6.3+ | **adapt** (P3) |
| ask: 2D puzzle in Go (Ebitengine) | ebitengine-game + puzzle | Ebitengine | reject |
| ask: Defold player movement | defold-core + input-systems | Defold | reject |
| ask: procedural dungeon in Godot | godot-tilemap + procedural-gen + roguelike | Godot 4.7 | reject |
| ask: cohesive pixel-art set | create-game-assets + engine importer | multi | reject |
| ask: save slots | save-systems | agnostic | **adapt** (P3) |
| ask: itch.io publish with butler | itch-publish | agnostic | reject |

### 4.7 `Claude-Code-Game-Studios` — 74 skills

All are Claude Code slash-command skills. `Harness` is `port` for every row: none runs as-is in OpenCode or Antigravity.

| Skill | Purpose | Category | Verdict |
|---|---|---|---|
| adopt | Brownfield audit — do existing artifacts actually work? Numbered migration plan. Unlike /project-stage-detect, checks... | production | reject |
| architecture-decision | Create an ADR documenting a technical decision: context, alternatives considered, consequences. | docs | reject |
| architecture-review | Traceability matrix mapping GDD requirements to ADRs. Finds gaps, cross-ADR conflicts, engine compatibility.... | docs | reject |
| art-bible | Author the Art Bible — visual identity gating asset production. Run before /map-systems. | art | reject |
| asset-audit | Audit assets against naming conventions, file size budgets, format standards. Finds orphaned assets, missing references. | art | reject |
| asset-spec | Per-asset visual specs plus AI generation prompts from GDDs and character profiles. After the art bible. | art | reject |
| balance-check | Find balance outliers, broken progressions, degenerate strategies, economy imbalances in formulas and data. 'Check game... | gameplay | reject |
| brainstorm | Guides the user through finding and choosing a game concept. Use when the user wants game ideas, a new concept, a game jam... | production | reject |
| bug-report | Structured bug report from a description, or analyze code for potential bugs. Reproduction steps, severity. | testing | reject |
| bug-triage | Re-evaluate open bugs — priority vs severity, assign to sprints, surface systemic trends. Run when the count grows. | testing | reject |
| changelog | Auto-generate a changelog from git commits and sprint data. Internal and player-facing versions. | production | reject |
| code-review | Architectural code review — coding standards, SOLID, testability, performance concerns. | docs | reject |
| consistency-check | Scan GDDs against the entity registry for cross-document conflicts. Grep-first approach targets conflicting sections,... | docs | reject |
| content-audit | Audit GDD content counts against what's implemented — planned vs built. | production | reject |
| create-architecture | Author the architecture blueprint before code is written. Validates decisions against the pinned engine, flags knowledge... | docs | reject |
| create-control-manifest | Builds a short list of must-do and never-do coding rules per system from accepted ADRs. Use when the user wants programmer... | docs | reject |
| create-epics | Turns the GDDs and architecture into epics, one per architectural module. Use when the user wants to break the game into... | production | reject |
| create-stories | Breaks one epic into implementable stories with acceptance criteria. Use when the user wants stories for an epic or the... | production | reject |
| day-one-patch | Day-one launch patch — focused fix for known issues found after gold master. Mini-sprint with QA gate and rollback. | production | reject |
| design-review | Reviews one design document for completeness, internal consistency, implementability, and design standards. Before handing... | docs | reject |
| design-system | Section-by-section GDD authoring for one system — walks through each required section, cross-references dependencies. | docs | reject |
| dev-story | Implements one story in code with tests, following its ADRs. Use when the user says to build, implement or start working on... | production | reject |
| estimate | Estimate task effort from complexity, dependencies, velocity, risk. Structured estimate with confidence levels. | production | reject |
| gate-check | Ready to advance between development phases? PASS/CONCERNS/NOT ASSESSED/FAIL with blockers and required artifacts. 'Can we... | production | reject |
| help | What should I do next? Use when stuck or you don't know what to do. | docs | reject |
| hotfix | Emergency fix bypassing normal sprint process — hotfix branch, approvals tracked, backport verified, full audit trail. | production | reject |
| launch-checklist | Launch readiness across every department: code, content, store, marketing, community, infrastructure, legal, go/no-go... | production | reject |
| localize | Localization pipeline — find hardcoded strings, extract string tables, cultural review, VO, RTL, enforce string freeze. | production | reject |
| map-systems | Decompose a concept into individual systems, map dependencies, prioritize design order, create the systems index. | production | reject |
| milestone-review | Milestone progress review — completeness, quality metrics, risk, go/no-go recommendation. At checkpoints or before a... | production | reject |
| onboard | Onboarding doc for a new contributor or agent — project state, conventions, priorities relevant to the specified role. | docs | reject |
| patch-notes | Player-facing patch notes from git history and changelogs. Translates developer language into player communication. | production | reject |
| perf-profile | Performance profiling — find bottlenecks, measure against budgets, produce ranked optimization recommendations. | engine | reject |
| playtest-report | Structured playtest report template, or turn existing playtest notes into structured feedback. | testing | reject |
| project-stage-detect | Analyze project state, detect stage, identify gaps, recommend next steps. 'Where are we in development? | production | reject |
| propagate-design-change | Finds architecture decisions (ADRs) made stale by a changed GDD and walks through resolving each. Use when the user changed... | docs | reject |
| prototype | Concept prototype before GDDs — throwaway HTML, Engine or Paper build, PROCEED/PIVOT/KILL/NOT ASSESSED. After /brainstorm... | production | reject |
| qa-plan | QA test plan for a sprint — classifies stories by Logic/Integration/Visual/UI, covers automated tests, manual cases, smoke... | testing | reject |
| quick-design | Lightweight spec for small changes — tuning adjustments, minor mechanics. Embeds directly into stories; skips full GDD. | docs | reject |
| regression-suite | Map test coverage to GDD critical paths, find fixed bugs lacking regression tests, flag drift from new features. | testing | reject |
| release-checklist | Pre-release checklist — build verification, certification requirements, store metadata, launch readiness. | production | reject |
| retrospective | Sprint or milestone retrospective from completed work, velocity, blockers. Actionable insights for the next iteration. | production | reject |
| reverse-document | Generate missing design or architecture docs from existing implementation — works backwards from code and prototypes. | docs | reject |
| review-all-gdds | Holistic cross-GDD review — contradictions between systems, dominant strategies, economic imbalance, cognitive overload,... | docs | reject |
| scope-check | Scope creep check — current scope versus the original plan. Flags additions, quantifies bloat, recommends cuts. 'Any scope... | production | reject |
| security-audit | Security audit — save tampering, cheat vectors, network exploits, data exposure, input validation. Before public or... | testing | reject |
| settings | View or change project config — effective merged values, or set locally in project.local.yaml. | production | reject |
| setup-engine | Configure engine and version. Pins it in CLAUDE.md; WebSearch fills reference docs when the version is beyond LLM training... | production | reject |
| skill-improve | Improve a skill via a test-fix-retest loop — static checks, targeted fixes, keep or revert on score change. | production | reject |
| skill-test | Validate skill files for structural compliance and behavioral correctness. Four modes: static linter, spec, category... | production | reject |
| smoke-check | Critical-path smoke gate before QA hand-off — runs the automated suite. A failed check means the build is not QA-ready. | testing | reject |
| soak-test | Soak test protocol for extended play — what to observe and log for slow leaks, fatigue, late-appearing edge cases. | testing | **adapt** (P3) |
| sprint-plan | New or updated sprint plan from the current milestone, completed work, and available capacity. | production | reject |
| sprint-status | Fast, concise sprint snapshot — burndown and emerging risks for situational awareness. 'How is the sprint going? | production | reject |
| start | Guides a first-time user through onboarding and picks the right workflow. Use when the user is new, just cloned or... | production | reject |
| story-done | Closes a finished story: verifies each acceptance criterion, checks for design or ADR deviations and updates its status.... | production | reject |
| story-readiness | Checks whether a story is ready to implement: clear acceptance criteria, no open questions, valid ADR and dependencies. Use... | production | reject |
| team-audio | Orchestrate the audio team — audio-director, sound-designer, technical-artist, gameplay-programmer — direction through... | production | reject |
| team-combat | Orchestrate the combat team — game-designer, gameplay-programmer, ai-programmer, technical-artist, sound-designer,... | production | reject |
| team-level | Orchestrate the level team — level-designer, narrative-director, world-builder, art-director, systems-designer, qa-tester —... | production | reject |
| team-live-ops | Orchestrate the live-ops team — live-ops-designer, economy-designer, analytics-engineer, community-manager, writer — for a... | production | reject |
| team-narrative | Orchestrate the narrative team — narrative-director, writer, world-builder, level-designer — for story, world lore,... | production | reject |
| team-polish | Orchestrate the polish team — performance-analyst, technical-artist, sound-designer, qa-tester — to optimize and harden a... | production | reject |
| team-qa | Orchestrate the QA team through a full testing cycle — qa-lead strategy and test plan, qa-tester case writing, execution,... | testing | reject |
| team-release | Orchestrate the release team — release-manager, qa-lead, devops-engineer, producer — to execute a release from candidate to... | production | reject |
| team-ui | Orchestrate the UI team through the UX pipeline — authoring, visual design, implementation, review, polish. Uses... | ui | reject |
| tech-debt | Track, categorize and prioritize technical debt across the codebase — scans for debt indicators, maintains a register. | production | reject |
| test-evidence-review | Quality review of test files and evidence — goes beyond existence, evaluates assertion coverage.... | testing | reject |
| test-flakiness | Find flaky tests from CI logs — aggregates pass rates, spots intermittent failures, recommends quarantine. After multiple... | testing | reject |
| test-helpers | Generate engine-specific test helper libraries — assertion utilities, factory functions, mocks in the engine's test folder.... | testing | **adapt** (P2) |
| test-setup | Scaffold the test framework and CI — tests/ directory, engine test runner, GitHub Actions workflow. Once, before the first... | testing | reject |
| ux-design | Section-by-section UX spec authoring for a screen, flow or HUD. Reads the player journey to provide context; also... | ui | reject |
| ux-review | Validate a UX spec, HUD design or pattern library — accessibility, GDD alignment, readiness. APPROVED / NOT ASSESSED /... | ui | reject |
| vertical-slice | Pre-production validation — end-to-end build to confirm the full loop is achievable before committing to Production. After... | production | reject |

**Support assets (not skills).** 18 hook scripts (`detect-gaps`, `log-agent`, `log-agent-stop`, `log-instructions`, `notify`, `pre-compact`, `session-start`, `session-stop`, `update-check`, `validate-agent-artifact`, `validate-assets`, `validate-commit`, `validate-push`, `validate-read`, `validate-registry`, `validate-skill-change`, `validate-write`, `yaml-helper`); 14 path-scoped rules (`agent-memory`, `ai-code`, `data-files`, `design-docs`, `engine-code`, `gameplay-code`, `narrative`, `network-code`, `prototype-code`, `scene-files`, `shader-code`, `skill-authoring`, `test-standards`, `ui-code`); 35 scripts (`adr-*`, `story-*`, `test-*`, `review-*`, `artifact-*`, `session-*`, `project-*`, guard scripts); 71 template files (GDD, art bible, UX spec, HUD, ADR, architecture, sprint, story, test plan, evidence, release, certification, patch notes, onboarding); 46 engine-reference files (Unity/Godot/Unreal modules and plugins, including Addressables, Cinemachine, DOTS Entities, GAS, CommonUI, PCG); `studio-tests/` with 74 skill specs, 49 agent specs, a catalog and a quality rubric.

### 4.8 `Claude-Code-Game-Studios` — 49 agents

All are Claude Code subagents; `Harness` is `port` for every row.

| Agent | Role | Engine | Verdict |
|---|---|---|---|
| accessibility-specialist | Accessibility standards and assistive features — remapping, text scaling, colorblind modes, screen reader support. | agnostic | **adapt** (P2) |
| ai-programmer | Implements game AI — behavior trees, state machines, pathfinding, perception, decision-making, NPC and enemy behavior, AI... | agnostic | reject |
| analytics-engineer | Telemetry and analytics — event tracking, player behavior, A/B test frameworks, dashboard specification, data pipelines. | agnostic | reject |
| art-director | Owns visual identity — style guides, art bible, asset standards, color palettes, UI visual direction. | agnostic | reject |
| audio-director | Owns sonic identity — music direction, sound palette, audio implementation strategy, mix balance. | agnostic | reject |
| community-manager | Player-facing communication — patch notes, social posts, community updates, feedback collection, player bug triage, crisis... | agnostic | reject |
| creative-director | Highest creative authority — vision, tone, aesthetic direction. Use when a decision affects game identity or department... | agnostic | reject |
| devops-engineer | Build pipelines, CI/CD configuration, version control workflow, branching strategy, automated test pipelines, deployment... | agnostic | reject |
| economy-designer | Resource economies — loot tables, sink/faucet analysis, progression curve calibration, economic balance. | agnostic | reject |
| engine-programmer | Core engine systems — rendering pipeline, physics, memory, resource loading, scene management, performance-critical... | agnostic | reject |
| game-designer | Mechanical and systems design — core loops, progression, combat, economy, player-facing rules. Mechanics-level questions. | agnostic | reject |
| gameplay-programmer | Implements designed mechanics as code — player systems, combat, interactive features, gameplay system code. | agnostic | reject |
| godot-csharp-specialist | Godot 4 C# quality — .NET patterns, attribute-based exports, signal delegates, async patterns, type-safe node access. | Godot 4 | reject |
| godot-gdextension-specialist | Native code integration with Godot — GDExtension API, godot-cpp and godot-rust bindings, custom node types, the native... | Godot 4 | reject |
| godot-gdscript-specialist | GDScript code quality — static typing enforcement, signal architecture, coroutine patterns, GDScript idioms, typed and... | Godot 4 | reject |
| godot-shader-specialist | Godot rendering customization — shading language, visual shaders, material setup, particle shaders, post-processing. | Godot 4 | reject |
| godot-specialist | Authority on Godot-specific patterns and APIs — node/scene architecture, signals, resources; guides GDScript vs C# vs... | Godot 4 | reject |
| lead-programmer | Lead programmer — code-level architecture, coding standards, code review, API design, refactoring strategy, assigning work... | agnostic | reject |
| level-designer | Spatial design for levels and areas — encounter layouts, pacing, difficulty, layout planning, environmental storytelling. | agnostic | reject |
| live-ops-designer | Post-launch content — seasonal events, battle passes, content cadence, retention, engagement analytics. Keeps the game fresh. | agnostic | reject |
| localization-lead | Internationalization architecture — i18n system design, string extraction workflows, locale testing, locale-specific... | agnostic | reject |
| narrative-director | Story architecture, world-building, character design, dialogue strategy. Narrative structure and direction rather than... | agnostic | reject |
| network-programmer | Multiplayer networking — state replication, lag compensation, matchmaking, network protocol, netcode and bandwidth... | agnostic | reject |
| performance-analyst | Performance analyst — profiling, bottlenecks, memory analysis, frame time investigation, recommends optimizations, tracks... | agnostic | reject |
| producer | Production concerns — sprint planning, milestone tracking, risk, scope, cross-department coordination when multiple... | agnostic | reject |
| prototyper | Throwaway builds — concept prototypes after brainstorm to test an idea is fun before GDDs; vertical slices pre-production.... | agnostic | reject |
| qa-lead | Test strategy and process — test plan creation, bug severity assessment, regression planning, release quality gates,... | agnostic | reject |
| qa-tester | Writes test cases, bug reports, and checklists — test case generation, regression checklist creation, execution... | agnostic | reject |
| release-manager | Release pipeline — certification checklists, store submissions and page prep, platform requirements, version numbering,... | agnostic | reject |
| security-engineer | Protects against cheating, exploits, breaches — anti-cheat measures, secure save data and network comms, player privacy... | agnostic | reject |
| sound-designer | SFX specifications and spec sheets — audio event documentation, mixing parameters, sound category definitions. | agnostic | reject |
| systems-designer | Detailed subsystem mechanics — combat formulas, progression curves, crafting recipes, status effect interactions,... | agnostic | reject |
| technical-artist | Technical artist bridging art and engineering — shaders, VFX, rendering optimization, art pipeline tools, art-to-engine... | agnostic | reject |
| technical-director | High-level technical decisions — engine architecture, technology choices, performance strategy, technical risk,... | agnostic | reject |
| tools-programmer | Internal dev tools — editor extensions, content authoring tools, debug utilities, pipeline automation, editor workflow. | agnostic | reject |
| ue-blueprint-specialist | Blueprint architecture and Blueprint/C++ boundary — optimization, maintainable graphs. Prevents Blueprint spaghetti,... | Unreal 5 | reject |
| ue-gas-specialist | Gameplay Ability System — abilities, gameplay effects, attribute sets, tags, ability tasks, prediction. Prevents common GAS... | Unreal 5 | reject |
| ue-replication-specialist | Unreal networking — property replication, RPCs, client prediction, relevancy, net serialization, bandwidth.... | Unreal 5 | reject |
| ue-umg-specialist | Unreal UI implementation — UMG widget hierarchy, data binding, CommonUI input routing, widget styling, UI optimization. | Unreal 5 | reject |
| ui-programmer | Implements user interface systems — menus, HUDs, inventory screens, dialogue boxes, UI framework code, screen flow. | agnostic | reject |
| unity-addressables-specialist | Unity asset management — Addressable groups, loading/unloading, content catalogs, remote delivery, asset bundle... | Unity 6.x | reject |
| unity-dots-specialist | Unity Data-Oriented Technology Stack — Entity Component System, Jobs, Burst compiler optimization, hybrid renderer, correct... | Unity 6.x | **adapt** (P1) |
| unity-shader-specialist | Unity rendering — Shader Graph, custom HLSL, VFX Graph, render pipeline customization (URP/HDRP), post-processing within... | Unity 6.x | reject |
| unity-specialist | Authority on Unity-specific patterns and APIs — guides MonoBehaviour vs DOTS/ECS, Unity subsystems including Addressables... | Unity 6.x | reject |
| unity-ui-specialist | Unity UI implementation — UI Toolkit (UXML/USS), UGUI Canvas, data binding, runtime UI performance, cross-platform... | Unity 6.x | **adapt** (P1) |
| unreal-specialist | Authority on Unreal-specific patterns and APIs — guides Blueprint vs C++, UE subsystems (GAS, Enhanced Input, Niagara, etc.). | Unreal 5 | reject |
| ux-designer | User experience flows, interaction design, information architecture — user flow mapping, interaction pattern design,... | agnostic | reject |
| world-builder | World builder for lore — factions, cultures, history, geography, ecology, world rules. Faction design, lore consistency... | agnostic | reject |
| writer | Writer of player-facing text — dialogue, lore entries, item and ability descriptions, environmental text. | agnostic | reject |

---

## 5. Compatibility matrix: candidates vs this project's actual needs

| Project need | Current state | Candidate | Verdict |
|---|---|---|---|
| UI Toolkit screens | 9+ UXML/USS screens in `A&B-U/Assets/UI` (`Main`, `Shop`, `Factory`, `CombatHud`, `Options`, `Info`, `Ready`, `Center`, `HangarPanelSettings`) | `game-ui-ux`; `unity-ui-specialist` | **adopt** `game-ui-ux`; **adapt** the agent (UI Toolkit/UGUI, project naming) |
| UGUI | `com.unity.ugui` installed but no Canvas screens found | `unity-ui-specialist` covers both | adapt only; no UGUI-specific skill needed |
| DOTS / Jobs / Burst / NativeArray | `ShipMovementJob`, `ProjectileMoveJob`, `TacticalAIJob` use `IJobParallelFor` + `[BurstCompile]`; no `com.unity.entities` | `unity-dots-specialist`; `performance-optimization` | **adapt** the agent (drop ECS half); **adopt** performance |
| PhysicsCore2D | `com.unity.modules.physicscore2d` 1.0.0; struct/raycast API (ADR 0014) | `physics-tuning`; `unity-physics` | **adapt** `physics-tuning`; **reject** `unity-physics` (3D Rigidbody) |
| Input System | `com.unity.inputsystem` 1.20.1; mobile touch/gamepad not yet specified | `unity-input-system`; `input-systems` | **adopt** `unity-input-system`; reject `input-systems` as duplicate for now |
| ScriptableObject catalogs | `PartDefinitionSO`, `CraftBlueprintSO`, `PartCatalogImporter` | `unity-scriptableobjects` | **adopt** |
| Original asset extraction / atlas / 9-slice | `scripts/export-ui-sprites.bat`, `export_all_assets.py`, `docs/assets/extraction-manifest.json`; ticket #124 | `game-ui-*` pipeline; `create-game-assets` | **reject** the game-ui pipeline (duplicate + ADR 0020); **adapt** only the manifest/provenance/normalization half of `create-game-assets` |
| Tier 1 / Tier 2 tests | `dotnet test` (~150 ms) + headless UTF (ADR 0016); 64 test files (280 `[Test]`/`[TestCase]` attributes) | `test-helpers`; `test-setup`, `smoke-check`, `qa-plan`, `regression-suite`, `test-evidence-review`, `test-flakiness` | **adapt** `test-helpers`; reject the rest as duplicates of the existing gates |
| ADRs / domain docs / architecture | `docs/adr/` 0001-0022, `GLOSSARY.md`, `CODING_STANDARDS.md`, graphify | `architecture-decision`, `architecture-review`, `create-architecture`, `reverse-document`, `consistency-check` | **reject** all: duplicate the accepted ADR/domain process |
| 60 FPS / 16.67 ms / zero-alloc | ADR 0010 + CODING_STANDARDS + `BatchSpriteRenderer` | `performance-optimization`; `perf-profile` | **adopt** the former; reject the latter as duplicate |
| Combat camera | ADR 0005 configurable distance | `camera-systems` | **adapt** (follow/deadzone/framing) |
| Combat feel / telemetry | ADR 0004 telemetry, ADR 0022 projectile visuals, faction palette | `game-feel` | **adapt** (hit-stop/shake using original palette) |
| Captain tactical AI | ADR 0006 modes; `TacticalAIJob` | `game-ai`; `ai-behavior-trees-utility-ai` | **adapt** `game-ai`; reject BT/Utility (YAGNI) |
| Audio remastering | "Not yet specified" in #122 | `audio-design` | **adapt** when specified (P3) |
| Save / mobile persistence | `docs/research/save-persistence-on-device.md`, `ShopSaveTests` | `save-systems` | **adapt** when a save ticket exists (P3) |
| Procedural mission waves | "Not yet specified" in #122 | `procedural-gen` | **adapt** when the wave director is specified (P3) |
| Build / CI / IL2CPP | Android package script, ADR 0016 headless gate | `unity-build-pipeline` | **adapt** (drop Addressables) |
| Accessibility | `com.unity.modules.accessibility` installed; no audit yet | `accessibility-specialist` | **adapt** (P2) |
| Studio roles / sprints / GDDs | GitHub Issues + wayfinder + to-spec/to-tickets | CCGS `team-*`, `create-*`, `sprint-*`, `dev-story` | **reject** all: process conflict and forbidden plan artifacts |

---

## 6. Image-generation skills and external-account dependency

| Skill | What it generates | External dependency | Credentials in repo | Decision |
|---|---|---|---|---|
| `game-ui-page-generator` | Full UI page PNG from a brief/prompt | Host/agent image generation tool or model account; no API client bundled | None | reject (ADR 0020) |
| `game-ui-component-breakdown` | Component PNGs (buttons, icons, cards) | Same external image tool/account | None | reject (ADR 0020) |
| `game-ui-workflow` | Orchestrates the two above | Same | None | reject (ADR 0020) |
| `create-game-assets` (awesome) | Optional sprite/tile/UI families; otherwise a brief + manifest | Optional `imagegen` skill or external tool; scripts use Pillow only | None | adapt non-generation half only |
| `asset-spec` (CCGS) | Prompt text only; explicitly does not call an image API | None today; live generation would require an external tool | None | reject (ADR 0020) |
| `art-bible` (CCGS) | Visual identity document, no images | None | None | reject (ADR 0020) |

**Policy for any future image generation:** an external account/subscription may be required, but only the fact "external account required" may appear in this repository. No key, token, account name, subscription ID, billing detail or signed URL may be committed. Generation outputs must not replace original Flash art (ADR 0020); if ever used, they are limited to procedural VFX/placeholder work with the original palette and must carry provenance per the `create-game-assets` manifest model.

---

## 7. Duplicates and conflicts with skills already used here

| Existing skill / process | Overlapping candidates | Resolution |
|---|---|---|
| **Hallmark** (visual design for pages) | `game-ui-design-workflow` (all), CCGS `art-bible`, `ux-design`, `ux-review` | Reject the visual-design ones; Hallmark is not a game-UI layout tool, and ADR 0020 fixes the original look. `game-ui-ux` is architecture, not visual redesign — no conflict. |
| **research** | None | No overlap. |
| **grilling** | CCGS `brainstorm`, `design-review`, `gate-check`, `quick-design` | Reject: they encode a different design-review ceremony and assume GDDs. |
| **TDD** | CCGS `dev-story`, `qa-plan`, `smoke-check`, `regression-suite`, `test-evidence-review`, `test-flakiness`, `test-setup` | Reject the process skills; keep only `test-helpers` (Unity helper generation) as an adapt. |
| **code review** | CCGS `code-review`, `security-audit`, `architecture-review`, `create-control-manifest` | Reject: duplicate review gates; `CODING_STANDARDS.md` is the project's review contract. |
| **graphify** | CCGS `architecture-review`, `reverse-document`, `consistency-check` | Reject: graph queries and `docs/adr/` already answer architecture questions. |
| **domain-modeling** | CCGS `design-system`, `map-systems`, `systems-designer` | Reject: the domain is already modeled in `GLOSSARY.md` and ADRs. |
| **wayfinder / to-spec / to-tickets** | CCGS `create-epics`, `create-stories`, `sprint-plan`, `sprint-status`, `story-*`, `estimate`, `scope-check`, `tech-debt`, `milestone-review`, `retrospective`, `vertical-slice` | Reject: AGENTS.md makes GitHub Issues the only tracker and forbids `implementation-plan.md`/`task-list.md` artifacts. |
| **pr** | CCGS `release-checklist`, `launch-checklist`, `hotfix`, `day-one-patch`, `changelog`, `patch-notes` | Reject: no store/release process or accounts. |
| **opencode** | None | No conflict; it was used to verify the discovery paths. |
| **Project zero-alloc rules** | `unity-csharp-scripting`, `unity-dots-specialist`, CCGS `engine-programmer` | Adapt: their coroutine/`new`/LINQ examples must be constrained by `CODING_STANDARDS.md` §2. |
| **Project asset fidelity (ADR 0020)** | `create-game-assets`, `asset-spec`, `art-bible`, all `game-ui-*` generation skills | Reject generation; adapt only provenance/manifest/normalization. |

---

## 8. Proposed project-local install layout

### 8.1 Directory tree

```
.agents/
  skills/                              # canonical, shared by OpenCode + Antigravity
    game-ui-ux/                        # adopt (Apache-2.0)
      SKILL.md
      UPSTREAM.md
      LICENSE
    performance-optimization/          # adopt
    unity-input-system/                # adopt
    unity-scriptableobjects/           # adopt
    physics-tuning/                    # adapt (P1)
    create-game-assets/                # adapt (P2; generation section removed)
    game-ai/                           # adapt (P2)
    game-feel/                         # adapt (P2)
    camera-systems/                    # adapt (P2)
    unity-csharp-scripting/            # adapt (P2)
    unity-build-pipeline/              # adapt (P2)
    test-helpers/                      # adapt (P2, from CCGS)
    audio-design/                      # adapt (P3)
    procedural-gen/                    # adapt (P3)
    save-systems/                      # adapt (P3)
    soak-test/                         # adapt (P3, from CCGS)
  agents/                              # Antigravity only (phase 2)
    ab-unity-ui-specialist.md
    ab-unity-dots-specialist.md
    ab-accessibility-specialist.md
.opencode/
  agents/                              # OpenCode only (phase 2; same bodies, different frontmatter)
    ab-unity-ui-specialist.md
    ab-unity-dots-specialist.md
    ab-accessibility-specialist.md
LICENSES/
  Apache-2.0.txt
  MIT.txt
docs/
  skills/
    ATTRIBUTION.md                     # source, copyright, license, modifications
    vendored-skills.lock.json          # pinned commits + local paths + patch notes
```

The tree is the target end state; phase 1 installs only the four `adopt` folders. Everything else waits for the grilling.

**Why this shape**

- **One skill tree, not two.** `.agents/skills/` is read by both harnesses today. A `.opencode/skills/` copy would be OpenCode-only and would need a duplicate or symlink for Antigravity, creating drift.
- **Flat folders.** Antigravity documents `<workspace-root>/.agents/skills/<skill-folder>/`; nested grouping is only documented for OpenCode. Keep the vendor identity in `UPSTREAM.md`, not in the path, until nesting is tested in Antigravity.
- **No global install.** Nothing goes to `~/.config/opencode/skills` or `~/.gemini/config/skills`; the ticket requires project scope.
- **Optional explicit OpenCode config.** If the compatibility path ever changes, add `{"skills": [".agents/skills"]}` to an `opencode.json` at the repo root. It is not required with current docs and is not proposed as part of phase 1.
- **Agents are phase 2.** They are valuable but cannot be shared: OpenCode wants `description`/`mode`/`permissions`, Antigravity wants `name`/`description`/`tools`/`subagent`/`commandExecutionPolicy`. Two files per agent is acceptable for three agents, not for the 49-agent studio.

### 8.2 Naming and frontmatter rules

- Skill IDs are the **folder names**, lowercase kebab-case, stable and unique. Upstream names are kept for unmodified copies; adapted copies that need a different trigger get an `ab-` prefix (e.g. `ab-physics-core2d` if `physics-tuning` is rewritten rather than edited).
- Every vendored `SKILL.md` keeps a valid `name` and a `description` that says what it does and when to use it. Do not add harness-specific frontmatter to the shared file.
- Adapted copies must not silently change the upstream description's meaning; record the change in `UPSTREAM.md`.
- If a skill name collides with a user-level skill (e.g. a future `game-ui-ux` in `~/.config/opencode/skills`), the project copy wins in OpenCode only if it is in `.opencode/skills`; with `.agents/skills` the precedence is lower. Resolve collisions by renaming the project copy, not by editing the user-level skill.
- **Precedence is not symmetric.** In OpenCode, a global `~/.config/opencode/skills/<id>` outranks project `.agents/skills/<id>`, while project `.opencode/skills` outranks both. Antigravity documents workspace and global scopes but not the same ordering. Keep IDs unique (or `ab-` prefixed) so a user-level skill cannot shadow a project skill.

### 8.3 Per-skill metadata

Every vendored folder carries:

```text
<skill-id>/
  SKILL.md
  UPSTREAM.md      # source URL, source path, commit SHA, license, vendored date, modifications
  LICENSE          # upstream license text (MIT or Apache-2.0)
  NOTICE           # only for Apache-2.0 upstreams that ship one (awesome)
```

`UPSTREAM.md` template:

```markdown
# Upstream

- Source: https://github.com/<owner>/<repo>
- Path: <path/to/skill>
- Commit: <40-char SHA>
- License: <MIT | Apache-2.0>
- Vendored: 2026-11-09
- Local modifications: <none | list, one per line>
```

### 8.4 Lockfile and attribution

`docs/skills/vendored-skills.lock.json` is the single source of truth for what is vendored:

```json
{
  "schemaVersion": 1,
  "skills": [
    {
      "id": "game-ui-ux",
      "sourceRepo": "https://github.com/gamedev-skills/awesome-gamedev-agent-skills",
      "sourcePath": "skills/disciplines/game-ui-ux",
      "commit": "19d93f97c9c1ea0f8b306e77aa81e0d3e352d3e9",
      "license": "Apache-2.0",
      "localPath": ".agents/skills/game-ui-ux",
      "modifications": "none",
      "adoptedAt": "2026-11-09"
    }
  ]
}
```

`docs/skills/ATTRIBUTION.md` lists each skill, its copyright holder, license, source URL and a one-line modification note. It is the human-readable twin of the lockfile.

**License obligations**

- **MIT** (game-ui, CCGS): include the copyright notice and permission notice in the vendored copy; mark modified files.
- **Apache-2.0** (awesome): include a copy of the license, retain the upstream `NOTICE` (copyright 2026 Abhishek Barali and contributors; trademark note), and state changes in modified files. A root `docs/skills/ATTRIBUTION.md` plus per-skill `LICENSE`/`NOTICE` satisfies this for a source distribution.
- The repository's own `LICENSE.md` (MIT fan tooling) is unaffected; third-party notices live in the files above and must not be merged into the project's copyright line.

### 8.5 Update policy

1. **Pin, never float.** Every vendored skill records a full commit SHA. No branch names, no `main`, no `npx skills update`.
2. **Update on a trigger, not a timer:** an upstream release that touches an adopted skill, a security/license change, or a quarterly review. The awesome repo itself reviews baselines quarterly.
3. **Diff before replacing.** Clone the new commit outside the repo, diff the skill folder, read the diff, then re-apply local modifications listed in `UPSTREAM.md`. Never overwrite an adapted skill blindly.
4. **Review scripts as code.** Any `scripts/` file is unreviewed third-party code. Do not execute it in CI or hooks until it has been read, pinned and (if kept) covered by a test. Phase 1 adopts Markdown-only skills, so this does not block.
5. **Re-run the checks:** frontmatter has `name` + `description`; folder name is stable; `UPSTREAM.md` commit updated; `LICENSE`/`NOTICE` present; `ATTRIBUTION.md` and lockfile updated; no credentials, tokens or account identifiers.
6. **Removal:** delete the skill folder, its lockfile entry and its attribution row; record the removal in the commit message. Do not leave a half-vendored folder.
7. **No auto-installers.** `install.sh`, `npx skills add`, Claude plugin marketplace commands and `agy plugin install` are out of bounds for this repo; copies are made by hand and reviewed.

### 8.6 Phase plan

| Phase | What | Why |
|---|---|---|
| 1 | Vendor the 4 adopt skills (`game-ui-ux`, `performance-optimization`, `unity-input-system`, `unity-scriptableobjects`) with `UPSTREAM.md`/`LICENSE`/lockfile/attribution | Zero content edits, Apache-2.0, immediate value, no scripts |
| 2 | Port `physics-tuning` and the three Unity/accessibility agents; adapt `create-game-assets`, `game-ai`, `game-feel`, `camera-systems`, `unity-csharp-scripting`, `unity-build-pipeline`, `test-helpers` | Requires edits and a per-harness agent copy; do after the adoption grilling |
| 3 | Defer `audio-design`, `procedural-gen`, `save-systems`, `soak-test` | Their project features are "not yet specified" in #122 |

---

## 9. Open questions for the adoption grilling

1. **Shared path vs OpenCode canonical path.** Do we accept `.agents/skills/` as the single source (both harnesses read it) or do we keep `.opencode/skills/` and duplicate/symlink for Antigravity? The former avoids drift; the latter follows OpenCode's canonical docs and its higher precedence.
2. **Antigravity nesting.** Official docs only promise `.agents/skills/<skill-folder>/`. Do we need a live Antigravity test before allowing `.agents/skills/awesome/<skill>/`, or do we keep flat folders permanently?
3. **Agents at all?** The three Unity/accessibility agents are valuable, but each needs two files (OpenCode and Antigravity frontmatter) and a body port. Is a per-harness duplicate acceptable, or do we write project-specific agents once and skip CCGS entirely?
4. **`game-ui-ux` vs ADR 0020.** The skill is architecture, not visual redesign, but its examples could tempt an agent to "improve" layout. Do we adopt it with an explicit "reference-only, do not restyle" note, or adapt it into a project-specific UI checklist?
5. **`create-game-assets` without generation.** Is the manifest/provenance/normalization half worth a vendored skill if the project already has `docs/assets/extraction-manifest.json` and `UiSpriteManifestTests`, or is it a reject?
6. **`physics-tuning` vs a project-specific PhysicsCore2D skill.** The generic skill speaks Rigidbody/fixed timestep; the project uses structs and raycasts. Do we adapt it or write a small `ab-physics-core2d` skill from ADR 0014/0017?
7. **`unity-dots-specialist` vs a Jobs/Burst skill.** The agent is half ECS (Entities) and half Jobs/Burst. Do we cut it down, or write a project skill that matches the actual `IJobParallelFor` + `NativeArray` code?
8. **`test-helpers` value.** The Tier 1 suite is pure C# with 64 test files. Which repeated setup/assertion patterns actually justify generated helpers? If none, reject it with the other CCGS QA skills.
9. **Naming and collisions.** Do we keep upstream IDs (`game-ui-ux`, `performance-optimization`) or prefix every vendored skill with `ab-` to avoid colliding with user-level skills and to make provenance obvious?
10. **License/attribution owner.** Who reviews the Apache-2.0 NOTICE and the modification notes, and where does the root `docs/skills/ATTRIBUTION.md` live relative to `LICENSE.md`?
11. **Script policy.** Phase 1 has no scripts, but `create-game-assets` and `test-helpers` can generate/execute code. Do we allow any bundled script to run, and under what review/sandbox rule?
12. **Validation gate.** Do we add a small validator (frontmatter present, `name` matches folder, `LICENSE`/`UPSTREAM.md` present, no credentials, no URLs in scripts) to the pre-commit hook, or is manual review enough?
13. **Trigger tests.** How do we prove a skill fires in both harnesses without executing external code? A fixture prompt per skill, a manual smoke, or a recorded transcript?
14. **Rejected inventory retention.** Do we keep the 196 rejects only in this research doc/manifest, or also as a `docs/skills/rejected.md` index so future agents do not re-investigate the same repos?
15. **Update cadence and ownership.** Who owns the quarterly diff and the decision to update/remove a skill when the upstream license or Unity baseline changes?

---

## 10. Sources

**Repositories (cloned, read-only):**

- <https://github.com/guiguiyan930-source/game-ui-design-workflow> @ `1ec604638dd1a27198576206eb3e47632725a078`
- <https://github.com/gamedev-skills/awesome-gamedev-agent-skills> @ `19d93f97c9c1ea0f8b306e77aa81e0d3e352d3e9`
- <https://github.com/Donchitos/Claude-Code-Game-Studios> @ `05ad7c8a5b8aa2aa86bbd3439ae7cf7295cac418`

**Official harness documentation (checked 2026-11-09):**

- OpenCode skills: <https://opencode.ai/docs/skills>
- OpenCode agents: <https://opencode.ai/docs/agents>
- Antigravity skills: <https://antigravity.google/docs/skills/>
- Antigravity subagents: <https://antigravity.google/docs/subagents/>

**Local project evidence:**

- `A&B-U/ProjectSettings/ProjectVersion.txt` — Unity `6000.6.8f1`
- `A&B-U/Packages/manifest.json` — URP 17.6.0, Input System 1.20.1, uGUI 2.6.0, PhysicsCore2D 1.0.0, uielements
- `A&B-U/Packages/packages-lock.json` — Burst 2.0.0, Collections 6.6.0, Mathematics 1.4.0; **no** `com.unity.entities`
- `A&B-U/Assets/UI/*.uxml` / `*.uss` — UI Toolkit screens
- `A&B-U/Assets/Scripts/Simulation/*Job.cs` — `IJobParallelFor` + `[BurstCompile]`
- `docs/adr/0014`, `0015`, `0016`, `0020`; `CODING_STANDARDS.md`; `AGENTS.md`

**Companion artifact:** `docs/research/game-dev-skills-manifest.json` (217 machine-readable records with `name`, `sourceRepo`, `path`, `engine`, `category`, `harnessSupport`, `dependencies`, `license`, `recommendation`, `reason`, plus `kind`, `priority`, `maintenance`, `sourceCommit`).
