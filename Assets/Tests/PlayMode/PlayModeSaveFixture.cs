using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

// DELIBERATELY OUTSIDE ANY NAMESPACE. An NUnit [SetUpFixture] applies to the namespace it sits
// in; at global scope it wraps EVERY test in the GatekeepersProtocol.PlayTests assembly. That is
// the whole point: PlayTestUtil.SaveFileGuard already existed and correctly backed up the
// developer's campaign, and not one test ever constructed it — so every Play-mode run wrote
// straight into gatekeeper_meta.json. Opt-in isolation is isolation that gets forgotten.
[SetUpFixture]
public sealed class PlayModeSaveFixture
{
    const string Tag = "[PlayModeSaveFixture]";

    static IDisposable _guard;

    /// <summary>
    /// Recovers a backup orphaned by a previous crashed run, then guards this one.
    /// </summary>
    /// <remarks>
    /// The recovery half matters as much as the guard. A run that dies mid-flight — a domain
    /// reload, a TestRunner error, the editor being closed — never reaches OneTimeTearDown, so
    /// the backup survives on disk and the live save stays whatever the tests left it. Restoring
    /// a stale backup here is what makes the guard crash-safe rather than merely correct.
    /// </remarks>
    [OneTimeSetUp]
    public void BackUpTheRealSave()
    {
        RecoverOrphanedBackup();
        string path = GatekeepersProtocol.PlayTests.PlayTestUtil.ResolveSavePathForFixture();
        _guard = new GatekeepersProtocol.PlayTests.PlayTestUtil.SaveFileGuard();
        if (!string.IsNullOrEmpty(path))
            Debug.Log($"{Tag} guarding campaign save for PlayMode suite.");
        else
            Debug.LogWarning($"{Tag} SaveService.SavePath unresolved — suite is not guarding a save.");
        if (string.IsNullOrEmpty(GatekeepersProtocol.PlayTests.PlayTestUtil.ResolveSaveRootForFixture()))
            Debug.LogWarning($"{Tag} SaveService.SaveRoot unresolved — the campaign directory is NOT guarded.");
    }

    [OneTimeTearDown]
    public void RestoreTheRealSave()
    {
        if (_guard == null) return;
        try
        {
            _guard.Dispose();
            Debug.Log($"{Tag} restored campaign save after PlayMode suite.");
        }
        catch (Exception e) { Debug.LogWarning($"{Tag} restore failed: {e.Message}"); }
        finally { _guard = null; }
    }

    static void RecoverOrphanedBackup()
    {
        RecoverOrphanedRootBackup();
        string path = GatekeepersProtocol.PlayTests.PlayTestUtil.ResolveSavePathForFixture();
        if (string.IsNullOrEmpty(path)) return;
        string backup = path + GatekeepersProtocol.PlayTests.PlayTestUtil.SaveFileGuard.BackupSuffix;
        if (!File.Exists(backup)) return;
        try
        {
            File.Copy(backup, path, true);
            File.Delete(backup);
            Debug.LogWarning($"{Tag} restored a save backup left by a crashed test run.");
        }
        catch (IOException e) { Debug.LogWarning($"{Tag} could not recover backup: {e.Message}"); }
        catch (UnauthorizedAccessException e) { Debug.LogWarning($"{Tag} could not recover backup: {e.Message}"); }
    }

    /// <summary>The campaign is a directory of Boarder and realm documents, and a crashed run
    /// leaves it moved aside exactly as it leaves the legacy file copied aside. Recovering only
    /// the file would restore the half nothing reads any more and abandon the half that is the
    /// actual campaign.</summary>
    static void RecoverOrphanedRootBackup()
    {
        string root = GatekeepersProtocol.PlayTests.PlayTestUtil.ResolveSaveRootForFixture();
        if (string.IsNullOrEmpty(root)) return;
        string backup = root + GatekeepersProtocol.PlayTests.PlayTestUtil.SaveFileGuard.BackupSuffix;
        if (!Directory.Exists(backup)) return;
        try
        {
            // Whatever is sitting in the live directory was written by the run that died, so it
            // is test residue and not a campaign. The backup is the campaign.
            if (Directory.Exists(root)) Directory.Delete(root, true);
            Directory.Move(backup, root);
            Debug.LogWarning($"{Tag} restored a campaign directory left by a crashed test run.");
        }
        catch (IOException e) { Debug.LogWarning($"{Tag} could not recover campaign: {e.Message}"); }
        catch (UnauthorizedAccessException e) { Debug.LogWarning($"{Tag} could not recover campaign: {e.Message}"); }
    }
}
