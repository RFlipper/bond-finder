using System;

class MoexBondDetails {
    public string Type;
    public DateTime IssueDate;
    public int CouponFreq = -1;
    public bool ForQualifiedInvestors;
    public int ListLevel = -1; // http://www.banki.ru/blog/BAY/8858.php
}

static class MoexBondDetailsExtensions {
    public static bool IsCorp(this MoexBondDetails b) {
        return b.Type != MoexApi.BOND_TYPE_OFZ && b.Type != MoexApi.BOND_TYPE_SUBFED;
    }

    public static string GetTypeText(this MoexBondDetails b) {
        switch(b.Type) {
            case MoexApi.BOND_TYPE_OFZ: return "ОФЗ";
            case MoexApi.BOND_TYPE_SUBFED: return "Муни";
            case MoexApi.BOND_TYPE_CORP: return "Корп";
            case MoexApi.BOND_TYPE_ETB: return "БО"; // http://www.banki.ru/wikibank/birjevaya_obligatsiya/
        }
        throw new NotSupportedException();
    }
}
