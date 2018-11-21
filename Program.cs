using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

class Program {

    static void Main(string[] args) {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        var analyzeDays = 5;
        var liqPercentile = 0.6m;
        var horizon = new DateTime(2022, 1, 1);

        var aggregatedBonds = EnumerateHistoryItems(DateTime.Now.Date.AddDays(-1), analyzeDays)
            .Where(r => r.MaturityDate > DateTime.Now.AddMonths(6) && r.MaturityDate < horizon)
            .Where(r => r.OfferDate == DateTime.MinValue && r.BuybackDate == DateTime.MinValue)
            .GroupBy(r => r.ID)
            .Select(g => new {
                ID = g.Key,

                g.First().ShortName,
                g.First().MaturityDate,
                g.First().FaceValue,

                TotalDays = g.Count(),
                TotalTrades = g.Sum(r => r.NumTrades),
                TotalValue = g.Sum(r => r.Value),
                g.OrderBy(r => r.TradeDate).Last().CloseYield,

                Details = new Lazy<MoexBondDetails>(() => MoexApi.GetBondDetails(g.Key))
            });

        // Убираем неликвид
        aggregatedBonds = aggregatedBonds.Where(b => b.TotalDays == analyzeDays);
        aggregatedBonds = aggregatedBonds.ToArray();
        var medianTransactions = Percentile(aggregatedBonds.Select(b => (decimal)b.TotalTrades), liqPercentile);
        aggregatedBonds = aggregatedBonds.Where(b => b.TotalTrades >= medianTransactions);

        // Оставляем доходности не меньше самой длинной ОФЗ
        aggregatedBonds = aggregatedBonds.ToArray();
        var longestOFZ = aggregatedBonds
            .Where(b => b.ShortName.StartsWith("ОФЗ"))
            .OrderByDescending(b => b.MaturityDate)
            .First();
        aggregatedBonds = aggregatedBonds.Where(b => b.CloseYield >= longestOFZ.CloseYield);

        // http://www.banki.ru/blog/BAY/9810.php "Налогообложение купонов"
        // "моментом эмиссии считается дата начала размещения облигаций"
        aggregatedBonds = aggregatedBonds.Where(b => !b.Details.Value.IsCorp() || b.Details.Value.IssueDate.Year >= 2017);

        aggregatedBonds = aggregatedBonds.Where(b => !b.Details.Value.ForQualifiedInvestors);

        aggregatedBonds = aggregatedBonds.OrderByDescending(b => b.CloseYield);

        var tsv = new StringBuilder();

        foreach(var b in aggregatedBonds) {
            tsv
                .AppendJoin("\t",
                    b.ShortName,
                    b.ID,
                    b.MaturityDate.ToString("yyyy-MM-dd"),
                    b.FaceValue,
                    b.Details.Value.CouponFreq,
                    (b.TotalValue / 1000000).ToString("N0") + "M",
                    b.CloseYield + "%"
                )
                .AppendLine();
        }

        Console.Write(tsv);

        Debugger.Break();
    }

    static IEnumerable<MoexBondHistoryItem> EnumerateHistoryItems(DateTime date, int dayCount) {
        for(var i = 0; i < dayCount; i++) {
            Console.Error.WriteLine("{0:ddd MM/dd}", date);

            var hasTrades = false;

            foreach(var board in new[] { MoexApi.EQOB, MoexApi.TQOB }) {
                foreach(var r in MoexApi.GetBondHistory(board, date) ) {
                    hasTrades = true;
                    yield return r;
                }
            }

            if(!hasTrades)
                dayCount++;

            date = date.AddDays(-1);
        }
    }

    // https://stackoverflow.com/a/8137526
    static decimal Percentile(IEnumerable<decimal> data, decimal percentile) {
        var sortedData = data.ToArray();
        Array.Sort(sortedData);

        var fracIndex = percentile * (sortedData.Length - 1);
        var intIndex = (int)fracIndex;
        var fracDiff = fracIndex - intIndex;

        if(intIndex + 1 < sortedData.Length)
            return sortedData[intIndex] * (1 - fracDiff) + sortedData[intIndex + 1] * fracDiff;

        return sortedData[intIndex];
    }

}
