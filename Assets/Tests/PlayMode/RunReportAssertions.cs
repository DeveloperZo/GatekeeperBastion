using System;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace GatekeeperBastion.PlayTests
{
    /// <summary>
    /// TEST-OWNED helper for the one sim member both end-screen tests have to read:
    /// RunSim.LastLossReport, shaped `{ string cause; long grossEarned; long repairSpent; }`.
    ///
    /// Every hop is taken by reflection on purpose. The sim is owned by another agent, and a
    /// direct field access here would turn any reshape of that struct into a compile error that
    /// takes the WHOLE PlayMode assembly down — including the death → lose screen → RETURN TO
    /// SHIP test, which is the one that must never stop running. Reflected, the same reshape
    /// fails exactly one assertion with a message that says what is missing and who reads it.
    ///
    /// It also pins the deletion: stakeName / kept / lost must NOT come back. The loss-stake
    /// mechanic is cut — enemies are worth currency, the currency banks on the kill, and a run
    /// report only says how it ended and what it made (gross) and what repair cost.
    /// </summary>
    public static class RunReportAssertions
    {
        /// <summary>
        /// Assert the settled-run report exists, names <paramref name="expectedCause"/>
        /// ("player" | "hatch" | "cleared"), reports a non-negative long 'grossEarned', exposes
        /// 'repairSpent', and exposes none of the deleted stake members. Pass
        /// <paramref name="expectedGross"/> to also pin the gross against the run tally
        /// captured before the run ended; pass null to skip it.
        /// </summary>
        public static void AssertShape(object sim, string expectedCause, long? expectedGross)
        {
            Assert.IsNotNull(sim, "No RunSim to read the run report from.");

            object report;
            if (!TryReadMember(sim, "LastLossReport", out report))
            {
                Assert.Fail("RunSim exposes no readable 'LastLossReport'. BOTH end screens read it " +
                            "for their summary line, so neither can say what the run was worth. " +
                            "Public members: " + MemberNames(sim.GetType()));
                return;
            }
            Assert.IsNotNull(report, "RunSim.LastLossReport read as null after the run ended.");
            Type reportType = report.GetType();

            object cause;
            if (!TryReadMember(report, "cause", out cause))
            {
                Assert.Fail($"The run report ({reportType.Name}) has no 'cause' member. The lose " +
                            "screen picks its headline (\"YOU DIED\" / \"HATCH DESTROYED\") from it, " +
                            "and the success screen distinguishes \"cleared\" by it. Public members: " +
                            MemberNames(reportType));
                return;
            }
            Assert.AreEqual(expectedCause, cause as string,
                $"The run report names the wrong cause. Expected '{expectedCause}'.");

            object gross;
            if (!TryReadMember(report, "grossEarned", out gross))
            {
                Assert.Fail($"The run report ({reportType.Name}) has no 'grossEarned' member. Both end " +
                            "screens print \"earned this run: N — already banked\" from gross and have " +
                            "nothing to print without it. Public members: " + MemberNames(reportType));
                return;
            }
            Assert.IsInstanceOf<long>(gross,
                "The run report's 'grossEarned' must be a whole salvage count (long); it is " +
                (gross == null ? "null" : gross.GetType().Name) + ".");

            long grossValue = (long)gross;
            Assert.GreaterOrEqual(grossValue, 0L, "The run report says the run earned less than nothing.");
            if (expectedGross.HasValue)
            {
                Assert.AreEqual(expectedGross.Value, grossValue,
                    $"The run report says gross {grossValue}, but the run tally when it ended was " +
                    $"{expectedGross.Value}. grossEarned must equal the floored kill awards.");
            }

            object repair;
            if (!TryReadMember(report, "repairSpent", out repair))
            {
                Assert.Fail($"The run report ({reportType.Name}) has no 'repairSpent' member. End " +
                            "screens show repair as its own line. Public members: " + MemberNames(reportType));
                return;
            }
            Assert.IsInstanceOf<long>(repair, "repairSpent must be a long.");

            // Pre-M22 field: a single pre-subtracted 'earned' must not return.
            object legacy;
            Assert.IsFalse(TryReadMember(report, "earned", out legacy),
                $"The run report ({reportType.Name}) still exposes 'earned'. M22 replaced it with " +
                "grossEarned + repairSpent; net is derived by the caller.");

            AssertStakeMembersStayDeleted(report, reportType);
        }

        /// <summary>The deleted mechanic must leave no trace on the report.</summary>
        static void AssertStakeMembersStayDeleted(object report, Type reportType)
        {
            object ignored;
            foreach (string gone in DeletedMembers)
            {
                Assert.IsFalse(TryReadMember(report, gone, out ignored),
                    $"The run report ({reportType.Name}) still exposes '{gone}'. The loss-stake " +
                    "mechanic is CUT: a run report is a cause, gross earned, and repair spent.");
            }
        }

        static readonly string[] DeletedMembers = { "stakeName", "kept", "lost" };

        /// <summary>Read a public instance field or property by exact name. Never throws.</summary>
        public static bool TryReadMember(object target, string name, out object value)
        {
            value = null;
            if (target == null || string.IsNullOrEmpty(name)) return false;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            Type type = target.GetType();
            try
            {
                FieldInfo field = type.GetField(name, flags);
                if (field != null) { value = field.GetValue(target); return true; }

                PropertyInfo prop = type.GetProperty(name, flags);
                if (prop != null && prop.CanRead && prop.GetIndexParameters().Length == 0)
                {
                    value = prop.GetValue(target, null);
                    return true;
                }
            }
            catch (System.Exception)
            {
                return false; // ambiguous match, or the getter threw — treat as absent
            }
            return false;
        }

        /// <summary>Public field + property names, so a failure message points somewhere useful.</summary>
        public static string MemberNames(Type type)
        {
            if (type == null) return "(unknown type)";
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            var sb = new StringBuilder();
            try
            {
                foreach (FieldInfo f in type.GetFields(flags))
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(f.Name);
                }
                foreach (PropertyInfo p in type.GetProperties(flags))
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(p.Name);
                }
            }
            catch (System.Exception) { /* best effort only — this is a diagnostic string */ }
            return sb.Length == 0 ? "(none)" : sb.ToString();
        }
    }
}

