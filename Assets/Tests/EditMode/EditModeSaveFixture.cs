using System;
using System.IO;
using GatekeepersProtocol.Game;
using NUnit.Framework;
using UnityEngine;

// DELIBERATELY OUTSIDE ANY NAMESPACE, for the same reason PlayModeSaveFixture is: an NUnit
// [SetUpFixture] applies to the namespace it sits in, and at global scope it wraps EVERY test
// in the GatekeepersProtocol.Tests assembly.
//
// The PlayMode assembly got a guard after the suite ate a developer's campaign once. EditMode
// never needed one, because no EditMode test wrote to disk — DevSessionTests backed up the one
// save file by hand and that was enough. Splitting the save moved the campaign into a
// DIRECTORY that GameSession.StartNew() writes two documents into, and that hand-rolled backup
// went on guarding a file nothing writes any more. Opt-in isolation is isolation that gets
// forgotten, so this is not opt-in.
[SetUpFixture]
public sealed class EditModeSaveFixture
{
    const string Tag = "[EditModeSaveFixture]";
    const string BackupSuffix = ".editmodebackup";
    const string LastRealmKey = "gkb.last_realm";
    const string LastBoarderKey = "gkb.last_boarder";

    static string _root;
    static string _backup;
    static bool _movedAside;
    static string _lastRealm;
    static string _lastBoarder;

    [OneTimeSetUp]
    public void MoveTheRealCampaignAside()
    {
        _lastRealm = PlayerPrefs.GetString(LastRealmKey, "");
        _lastBoarder = PlayerPrefs.GetString(LastBoarderKey, "");

        _root = SaveService.SaveRoot;
        if (string.IsNullOrEmpty(_root))
        {
            Debug.LogError($"{Tag} SaveService.SaveRoot did not resolve — the EditMode suite " +
                           "is NOT guarding the campaign. Tests that call GameSession.StartNew " +
                           "will write into it.");
            return;
        }

        _backup = _root + BackupSuffix;
        RecoverOrphan();
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Move(_root, _backup);
                _movedAside = true;
            }
            else
            {
                // Nothing to protect, but still note that the suite owns the directory now, so
                // teardown knows to clear whatever the tests create.
                _movedAside = true;
            }
        }
        catch (Exception e)
        {
            _movedAside = false;
            Debug.LogError($"{Tag} could not move the campaign aside ({e.GetType().Name}: " +
                           $"{e.Message}). The suite is NOT guarded and will write into it.");
        }
    }

    [OneTimeTearDown]
    public void PutItBack()
    {
        PlayerPrefs.SetString(LastRealmKey, _lastRealm ?? "");
        PlayerPrefs.SetString(LastBoarderKey, _lastBoarder ?? "");
        PlayerPrefs.Save();

        if (!_movedAside || string.IsNullOrEmpty(_root)) return;
        // Move the test's leavings aside before deleting them, and only delete once the real
        // campaign is safely back. Deleting first and restoring second means any failure in
        // between leaves the player with no campaign at all.
        string scratch = _root + ".editmodescratch";
        try
        {
            if (Directory.Exists(scratch)) Directory.Delete(scratch, true);
            if (Directory.Exists(_root)) Directory.Move(_root, scratch);
            if (!string.IsNullOrEmpty(_backup) && Directory.Exists(_backup))
                Directory.Move(_backup, _root);
            if (Directory.Exists(scratch)) Directory.Delete(scratch, true);
        }
        catch (Exception e)
        {
            Debug.LogError($"{Tag} could not restore the campaign ({e.GetType().Name}: " +
                           $"{e.Message}). It is at {_backup} — move it back by hand.");
        }
        finally
        {
            _movedAside = false;
        }
    }

    /// <summary>A run that died never reached teardown, so the campaign may still be sitting
    /// in the backup. Put it back before this run moves anything, and keep whatever is live
    /// rather than deleting it — it may be real play from since the crash.</summary>
    static void RecoverOrphan()
    {
        if (string.IsNullOrEmpty(_backup) || !Directory.Exists(_backup)) return;
        try
        {
            if (Directory.Exists(_root))
            {
                string stranded = _root + ".stranded";
                if (Directory.Exists(stranded)) Directory.Delete(stranded, true);
                Directory.Move(_root, stranded);
                Debug.LogWarning($"{Tag} a live campaign directory was set aside at {stranded} " +
                                 "so a backup from a crashed run could be restored. Neither was deleted.");
            }
            Directory.Move(_backup, _root);
            Debug.LogWarning($"{Tag} restored a campaign left behind by a crashed test run.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"{Tag} could not recover the crashed run's backup " +
                             $"({e.GetType().Name}: {e.Message}).");
        }
    }

    /// <summary>Empty the guarded directory between tests. Only safe because this fixture has
    /// already moved the real campaign out of the way; a test calling it unguarded would be
    /// deleting the developer's save.</summary>
    public static void ClearGuardedSaveRoot()
    {
        if (!_movedAside || string.IsNullOrEmpty(_root)) return;
        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
