namespace DSC.TLink.ITv2.Enumerations
{
    public enum ArmingMode : byte
    {
        Disarm = 0,
        StayArm = 1,
        AwayArm = 2,
        ArmWithNoEntryDelay = 3,
        NightArm = 4,
        QuickArm = 5,
        UserArm = 6,
        StayArmWithNoEntryDelay = 7,
        AwayArmWithNoEntryDelay = 8,
        NightArmWithNoEntryDelay = 9,
        GlobalStayArm = 11,
        GlobalAwayArm = 12,
        CustomizedArmingModeA = 13,
        CustomizedArmingModeB = 14,
        CustomizedArmingModeC = 15,
        CustomizedArmingModeD = 16,
        InstantStayArm = 17,
        ArmingHidden = 18,
        Unknown = 255,
    }
}
