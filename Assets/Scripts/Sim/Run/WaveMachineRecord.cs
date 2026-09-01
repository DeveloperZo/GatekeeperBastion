namespace GatekeepersProtocol.Sim
{
    /// <summary>The "charge/max" readout, built once and read forever. Two callers want the
    /// identical string for the identical integer: the ability strip, which asks every frame
    /// through the HUD's LateUpdate, and <see cref="WaveMachineRecord.Line"/>, which asks once
    /// a Wave. Concatenating at the call site handed the frame an allocation for a string that
    /// had already existed thousands of times, so the whole meter is spelled out up front.
    /// Both maxima are compile-time constants, which is what makes a fixed table possible at
    /// all — a runtime maximum would need a different answer.</summary>
    public static class MachineChargeLabels
    {
        static readonly string[] TurretLabels = Build(BoarderProtocol.TurretChargeMax);
        static readonly string[] OutriderLabels = Build(BoarderProtocol.OutriderChargeMax);

        /// <summary>Turret charge as "n/max", truncated toward zero the way the readout has
        /// always shown it.</summary>
        public static string OfTurret(float charge)
        {
            return At(TurretLabels, charge);
        }

        /// <summary>Outrider charge as "n/max". Separate from <see cref="OfTurret"/> rather
        /// than taking a maximum, so no caller can pair a charge with the wrong meter.</summary>
        public static string OfOutrider(float charge)
        {
            return At(OutriderLabels, charge);
        }

        /// <summary>Clamps rather than falling back to a fresh string. A charge outside its own
        /// meter is a caller defect, and formatting one here would quietly put the per-frame
        /// allocation back on exactly the path this table exists to keep clean.</summary>
        static string At(string[] table, float charge)
        {
            int n = (int)charge;
            if (n < 0) n = 0;
            if (n >= table.Length) n = table.Length - 1;
            return table[n];
        }

        static string[] Build(float max)
        {
            int top = (int)max;
            if (top < 0) top = 0;
            string denominator = "/" + top.ToString();
            var table = new string[top + 1];
            for (int i = 0; i < table.Length; i++) table[i] = i.ToString() + denominator;
            return table;
        }
    }

    /// <summary>What FORGE's two machines did across one Wave: which of them was standing
    /// first, and what each one's charge was when its special ran. One Boarding's log answers
    /// "which machine did this player lead with, and did they hold the special or burn it" from
    /// these lines alone, which is why the record is a run fact and not a HUD detail — it lives
    /// here so the harness can exercise the Wave-boundary rule below without an engine.</summary>
    public sealed class WaveMachineRecord
    {
        string _firstMachine = "";
        float _turretChargeAtSpecial = -1f;
        float _outriderChargeAtSpecial = -1f;
        bool _turretSpecialRan;
        bool _outriderSpecialRan;

        /// <summary>The machine named first this Wave, or "" while neither has been seen.</summary>
        public string FirstMachine => _firstMachine;

        /// <summary>Open a Wave. The caller passes what is standing right now, because a
        /// machine that survived the last Wave is this Wave's first without any deploy call
        /// arriving to say so.</summary>
        public void BeginWave(bool hasTurret, bool hasOutrider)
        {
            // A machine that stood through the last Assault is already this Wave's first.
            // Clearing the latch here made wave 2 of 2026-08-29_22-53-02 log outrider-first
            // after a turret that had been standing since wave 1.
            if (hasTurret && hasOutrider)
            {
                if (_firstMachine.Length == 0) _firstMachine = "turret";
            }
            else if (hasTurret)
                _firstMachine = "turret";
            else if (hasOutrider)
                _firstMachine = "outrider";
            else
                _firstMachine = "";
            _turretChargeAtSpecial = -1f;
            _outriderChargeAtSpecial = -1f;
            _turretSpecialRan = false;
            _outriderSpecialRan = false;
        }

        /// <summary>A machine just stood up. First name of the Wave wins: a later deploy is a
        /// second machine joining, never a correction of who led.</summary>
        public void NoteDeploy(string machine)
        {
            if (machine == null || machine.Length == 0) return;
            if (_firstMachine.Length == 0) _firstMachine = machine;
        }

        /// <summary>Charge at the instant Overcharge was cast — not the charge left afterwards,
        /// which the special immediately starts spending.</summary>
        public void NoteTurretSpecial(float charge)
        {
            _turretSpecialRan = true;
            _turretChargeAtSpecial = charge;
        }

        /// <summary>Charge at the instant EMP was cast. Latched independently of the turret's:
        /// either machine may run its special without the other having deployed.</summary>
        public void NoteOutriderSpecial(float charge)
        {
            _outriderSpecialRan = true;
            _outriderChargeAtSpecial = charge;
        }

        /// <summary>The Wave's line, or null when neither machine was ever up. "-" stands in
        /// for a special that never ran, so an absent cast reads differently from a cast at
        /// zero. The wording is load-bearing: offline readers grep `machines first=`, so the
        /// three keys and their order are a format other tools depend on, not a phrasing.
        /// </summary>
        public string Line()
        {
            if (_firstMachine.Length == 0) return null;
            string t = _turretSpecialRan
                ? MachineChargeLabels.OfTurret(_turretChargeAtSpecial)
                : "-";
            string o = _outriderSpecialRan
                ? MachineChargeLabels.OfOutrider(_outriderChargeAtSpecial)
                : "-";
            return "machines first=" + _firstMachine
                + " turret-special-charge=" + t
                + " outrider-special-charge=" + o;
        }
    }
}
