using System;

class MoexBondHistoryItem {
    public string ID;
    public string ShortName;
    public DateTime TradeDate;

    /// <summary>Непогашенный долг</summary>
    public decimal FaceValue;

    /// <summary>Количество сделок</summary>
    public int NumTrades;

    /// <summary>Объем в валюте</summary>
    public decimal Value;

    /// <summary>Доходность последней сделки</summary>
    public decimal CloseYield;

    /// <summary>Дата погашения</summary>
    public DateTime MaturityDate;

    /// <summary>Дата, к которой рассчитывается доходность (если данное поле не заполнено, то "Доходность посл.сделки" рассчитывается к Дате погашения)</summary>
    public DateTime BuybackDate;

    /// <summary>Дата оферты</summary>
    public DateTime OfferDate;
}
