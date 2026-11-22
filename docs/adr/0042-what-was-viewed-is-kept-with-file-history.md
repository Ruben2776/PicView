# What Was Viewed Is Kept With File History

The **Last Session** (the tabs of the last Main Window to close, which "Continue where you left off" opens at App Start), the **Last File** and the remembered start-up directory are kept in `FileHistory.json`, beside the **File History** entries, and not in the settings record. `LastFile` and `StartUpDirectory` move out of the record's `StartUp` group: the first run of the version that moves them reads them from the record once, writes them to `FileHistory.json` and drops them from the record. A `FileHistory.json` from an earlier version reads as before, with no Last Session. They are things the user viewed, not choices the user made, so they follow File History: with file history off none of them is recorded, "Open last file" and "Continue where you left off" are disabled and App Start shows the start-up menu, and clearing the history clears the Last File and the Last Session with it. The start-up choice itself stays a **Setting**: turning file history off keeps it, and turning it on again brings it back.

File History stays one concept and one module (ADR-0001): the Last Session is not a per-tab File History, and the dropdown lists the same entries as before.

## Considered Options

- **Keep the Last File in the record and put the Last Session next to it.** Nothing to migrate, but turning file history off would then have to reach into the record to stop and clear what was viewed, and the record would go on holding paths the user asked PicView to forget.
- **A per-tab File History ("TabFileHistory") that the Last Session is read from.** Each entry would have to name its tab across runs, though tabs are made new at every start and move between windows, and the dropdown would change for a feature that only needs one snapshot.

## Consequences

- Everything PicView remembers of what was viewed is in one file, which file history off and clearing the history cover whole.
- The settings record loses two properties; a settings file from before the move still loads, and its two values are migrated once.
- Renaming a file or folder renames it in the File History entries, the Last File and the Last Session, through `FileRenames` as before.
