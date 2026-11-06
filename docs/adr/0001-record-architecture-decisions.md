# 1. Record architecture decisions

Date: 2026-11-05

## Status
Accepted

## Context
We need a systematic way to record architectural and reverse-engineering decisions made during the revival, modding, and modernization of *Assembly and Battle: The Story in the Space*.

## Decision
We will use Architecture Decision Records (ADRs) as documented in `docs/adr/`. Each ADR documents:
1. Title and date
2. Status (Proposed, Accepted, Deprecated, Superseded)
3. Context of the problem
4. Decision taken
5. Consequences (positive and negative)

## Consequences
- Every significant architectural and engine modification is auditable by both humans and agents.
- Agents can navigate past decisions without guessing historical intent.

