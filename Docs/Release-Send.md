# Friends build — read this first

Extract the **whole folder** somewhere on disk (Desktop is fine). Do not run the `.exe` from inside the zip.

**SmartScreen** may say Windows protected your PC. Click **More info**, then **Run anyway**. The build is unsigned; that warning is expected.

If antivirus deletes or quarantines the exe, whitelist the extract folder (or restore from quarantine). Check the zip on VirusTotal before you send if you are the host.

**Firewall:** when Windows asks, allow on **Private and Public**. A host that ticks only Private is unreachable for many friends.

**Host EMBARKs first** (Ship → NAV → EMBARK) so the session is listening. The joiner EMBARKs after.

If something breaks, send a short note of what you did and attach:

`%USERPROFILE%\AppData\LocalLow\Gatekeeper\Gatekeeper Bastion\Player.log`

Stuck on a black screen or wrong monitor: run `run-windowed.bat` in the same folder as the exe.
