# Logs
One file per session: `GatekeeperBastion_yyyy-MM-dd_HH-mm-ss.log`. Editor: `<projectRoot>/Logs/` (gitignored). Player build: `Application.persistentDataPath/Logs/`.

- Line format: `[HH:mm:ss.fff] [Log|Warning|Error] [Tag] message` — wall clock, Unity's own level words. Stack traces follow errors and exceptions.
- Everything that reaches `Debug.*` lands in the file: our `Log.Event`/`Log.Metric`/`Log.Warn`/`Log.Error`/`Log.Info` (that last one needs the `GKB_VERBOSE` define), Unity's own warnings, package chatter. No filtering, no dedup.
- Newest 15 sessions kept; the file stays readable while Unity is running.
- Menu `GatekeeperBastion/Logs/`: Open Logs Folder · Copy Latest Log Path.
