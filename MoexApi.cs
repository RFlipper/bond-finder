using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

static class MoexApi {
    public const string
        // http://iss.moex.com/iss/engines/stock/markets/bonds/boards.txt
        EQOB = nameof(EQOB), // Т0 Облигации - безадрес.
        TQOB = nameof(TQOB); // Т+: Облигации - безадрес.

    public const string
        BOND_TYPE_OFZ = "ofz_bond",
        BOND_TYPE_SUBFED = "subfederal_bond";

    public static readonly IReadOnlyCollection<string> KNOWN_BOND_TYPES = new[] {
        BOND_TYPE_OFZ,
        BOND_TYPE_SUBFED,
        "corporate_bond",
        "exchange_bond"
    };

    // https://iss.moex.com/iss/reference/64
    public static IEnumerable<MoexBondHistoryItem> GetBondHistory(string board, DateTime date) {
        var start = 0;
        var limit = 100;

        while(true) {
            var url = $"https://iss.moex.com/iss/history/engines/stock/markets/bonds/boards/{board}/securities.json"
                + "?date=" + date.ToString("yyyy-MM-dd")
                + "&numtrades=1"
                + "&start=" + start
                + "&limit=" + limit;

            var text = CachingDownloader.Download(url, TimeSpan.FromDays(123));
            var obj = JsonConvert.DeserializeObject<JObject>(text);

            var columns = obj["history"]["columns"].ToObject<string[]>();
            var data = obj["history"]["data"];

            if(data.Count() < 1)
                break;

            var SECID = Array.IndexOf(columns, "SECID");
            var SHORTNAME = Array.IndexOf(columns, "SHORTNAME");
            var TRADEDATE = Array.IndexOf(columns, "TRADEDATE");
            var FACEVALUE = Array.IndexOf(columns, "FACEVALUE");
            var NUMTRADES = Array.IndexOf(columns, "NUMTRADES");
            var VALUE = Array.IndexOf(columns, "VALUE");
            var YIELDCLOSE = Array.IndexOf(columns, "YIELDCLOSE");
            var MATDATE = Array.IndexOf(columns, "MATDATE");
            var BUYBACKDATE = Array.IndexOf(columns, "BUYBACKDATE");
            var OFFERDATE = Array.IndexOf(columns, "OFFERDATE");

            foreach(var line in data) {
                yield return new MoexBondHistoryItem {
                    ID = To<String>(line[SECID]),
                    ShortName = To<String>(line[SHORTNAME]),
                    TradeDate = ToDate(line[TRADEDATE]),
                    FaceValue = To<Decimal>(line[FACEVALUE]),
                    NumTrades = To<Int32>(line[NUMTRADES]),
                    Value = To<Decimal>(line[VALUE]),
                    CloseYield = To<Decimal>(line[YIELDCLOSE]),
                    MaturityDate = ToDate(line[MATDATE]),
                    BuybackDate = ToDate(line[BUYBACKDATE]),
                    OfferDate = ToDate(line[OFFERDATE])
                };
            }

            start += limit;
        }
    }

    // https://iss.moex.com/iss/reference/13
    public static MoexBondDetails GetBondDetails(string id) {
        var text = CachingDownloader.Download($"https://iss.moex.com/iss/securities/{id}.json?iss.only=description", TimeSpan.FromHours(12));
        var obj = JsonConvert.DeserializeObject<JObject>(text);

        var result = new MoexBondDetails();

        foreach(var row in obj["description"]["data"]) {
            var value = row[2];
            switch(To<String>(row[0])) {
                case "TYPE":
                    result.Type = To<String>(value);
                    if(!KNOWN_BOND_TYPES.Contains(result.Type))
                        throw new NotSupportedException();
                    break;

                case "ISSUEDATE":
                    result.IssueDate = ToDate(value);
                    break;

                case "COUPONFREQUENCY":
                    result.CouponFreq = To<Int32>(value);
                    break;

                case "ISQUALIFIEDINVESTORS":
                    result.ForQualifiedInvestors = To<Int32>(value) == 1;
                    break;
            }
        }

        return result;
    }

    static DateTime ToDate(JToken token) {
        if(token.Type == JTokenType.Null)
            return DateTime.MinValue;

        return DateTime.ParseExact((string)token, "yyyy-MM-dd", null);
    }

    static T To<T>(JToken token) {
        if(token.Type == JTokenType.Null)
            return default(T);

        return (T)Convert.ChangeType(token, typeof(T));
    }

}
