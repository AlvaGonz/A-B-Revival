# Issue Tracker: GitHub Issues

This repository uses GitHub Issues as its canonical issue tracker.

## Operations

### Creating issues
```bash
gh issue create --title "<title>" --body "<body>" --label "<labels>"
```

### Listing issues
```bash
gh issue list --state open
```

### Viewing an issue
```bash
gh issue view <number>
```

### Wayfinding operations
- Map issue carries label `wayfinder:map`.
- Child tickets carry one of `wayfinder:task`, `wayfinder:research`, `wayfinder:prototype`, `wayfinder:grilling`.
- Claims: assign issue to self (`gh issue edit <number> --add-assignee "@me"`).
- Closing: `gh issue close <number> --comment "<resolution summary>"`.
