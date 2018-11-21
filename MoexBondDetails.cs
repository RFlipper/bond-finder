using System;

class MoexBondDetails {
    public string Type;
    public DateTime IssueDate;
    public int CouponFreq;
    public bool ForQualifiedInvestors;
}

static class MoexBondDetailsExtensions {
    public static bool IsCorp(this MoexBondDetails b) {
        return b.Type != MoexApi.BOND_TYPE_OFZ && b.Type != MoexApi.BOND_TYPE_SUBFED;
    }
}
