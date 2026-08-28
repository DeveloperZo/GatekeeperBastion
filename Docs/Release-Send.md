# Friends build — read this first

Extract the **whole folder** somewhere on disk (Desktop is fine). Do not run the `.exe` from inside the zip.

**SmartScreen** may say Windows protected your PC. Click **More info**, then **Run anyway**. The build is unsigned; that warning is expected.

If antivirus deletes or quarantines the exe, whitelist the extract folder (or restore from quarantine). Check the zip on VirusTotal before you send if you are the host.

**Firewall:** when Windows asks, allow on **Private and Public**. A host that ticks only Private is unreachable for many friends.

**Opening a world.** One of you hosts. On **REALMS**, pick or name a realm, press **OPEN TO FRIENDS**, and the join code appears with **COPY** beside it — send that code, then press **TO SHIP**. Nothing is started until you press it. If the code will not connect, **HOST ON LAN** is the fallback. Everyone else presses **JOIN** on the main menu and types the code.

**Going out together.** You all stand on the same Ship. At the **NAV MAP**, guests press **READY**; the party lead presses **EMBARK**, and a countdown takes the whole party across at once. Do not wait for a second EMBARK — there is only the one.

If something breaks, send a short note of what you did and attach:

`%USERPROFILE%\AppData\LocalLow\Gatekeeper\Gatekeeper's Protocol\Player.log`

Stuck on a black screen or wrong monitor: run `run-windowed.bat` in the same folder as the exe
(template: `Tools/release/run-windowed.bat` in the repo).
