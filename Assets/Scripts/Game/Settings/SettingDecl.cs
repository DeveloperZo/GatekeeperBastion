namespace GatekeeperBastion.Game
{
    /// <summary>One preferences row. The options screen draws exactly these — adding a
    /// setting is a row here, not layout code.</summary>
    public readonly struct SettingDecl
    {
        public readonly SettingId Id;
        public readonly string Label;
        public readonly SettingType Type;
        public readonly SettingCategory Category;
        public readonly bool DefaultBool;
        public readonly float DefaultFloat;
        public readonly int DefaultInt;
        public readonly float Min;
        public readonly float Max;
        public readonly string PrefKey;

        public SettingDecl(SettingId id, string label, SettingCategory category, bool defaultBool,
            string prefKey)
        {
            Id = id;
            Label = label;
            Type = SettingType.Bool;
            Category = category;
            DefaultBool = defaultBool;
            DefaultFloat = 0f;
            DefaultInt = 0;
            Min = 0f;
            Max = 0f;
            PrefKey = prefKey;
        }

        public SettingDecl(SettingId id, string label, SettingCategory category, float defaultFloat,
            float min, float max, string prefKey)
        {
            Id = id;
            Label = label;
            Type = SettingType.Float;
            Category = category;
            DefaultBool = false;
            DefaultFloat = defaultFloat;
            DefaultInt = 0;
            Min = min;
            Max = max;
            PrefKey = prefKey;
        }

        public SettingDecl(SettingId id, string label, SettingCategory category, int defaultInt,
            int min, int max, string prefKey)
        {
            Id = id;
            Label = label;
            Type = SettingType.Int;
            Category = category;
            DefaultBool = false;
            DefaultFloat = 0f;
            DefaultInt = defaultInt;
            Min = min;
            Max = max;
            PrefKey = prefKey;
        }
    }
}
