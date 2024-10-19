using TradingPlatform.BusinessLayer;

namespace SingleRangeBar.Strategy;

public class Strategy : TradingPlatform.BusinessLayer.Strategy
{
    private HistoricalData historicalData;

    [InputParameter("Symbol", 5)]
    public Symbol Symbol { get; set; }

    [InputParameter("Account", 10)]
    public Account Account { get; set; }
    
    [InputParameter("Range Bar Size (in Ticks)", 15)]
    public int RangeBarSize { get; set; } = 40;

    [InputParameter("# of Contracts", 20)]
    public int Contracts { get; set; } = 1;

    public Strategy()
    {
        Name = "Single Range Bar";
        Description = "Simple.  When it goes up, Buy.  When it goes down, Sell.";
    }

    protected override void OnRun()
    {
        historicalData = Symbol.GetHistory(new HistoryRequestParameters
        {
            Symbol = Symbol,
            FromTime = Core.Instance.TimeUtils.DateTimeUtcNow.AddHours(-5),
            ToTime = default,
            Aggregation = new HistoryAggregationRangeBars(RangeBarSize, Symbol.HistoryType)
        });

        historicalData.NewHistoryItem += OnNewHistoryItem;
    }

    private void OnNewHistoryItem(object sender, HistoryEventArgs e)
    {
        var orderParams = new PlaceOrderRequestParameters
        {
            Account = Account,
            Symbol = Symbol,
            Side = historicalData[1][PriceType.Open] < historicalData[1][PriceType.Close] ? Side.Buy : Side.Sell,
            TimeInForce = TimeInForce.Day,
            Quantity = Contracts,
            OrderTypeId = OrderType.Market
        };

        Flatten();
        Core.PlaceOrder(orderParams);
    }

    protected override void OnStop()
    {
        historicalData.NewHistoryItem -= OnNewHistoryItem;
    }

    private void Flatten()
    {
        foreach (var position in Core.Positions.Where(PositionWithSameAccountAndSymbol())) position.Close();
        foreach (var order in Core.Orders.Where(OrderWithSameAccountAndSymbol())) order.Cancel();
    }

    private Func<Position, bool> PositionWithSameAccountAndSymbol()
    {
        return position => position.Account.Equals(Account) && position.Symbol.Equals(Symbol);
    }
    
    private Func<Order, bool> OrderWithSameAccountAndSymbol()
    {
        return order => order.Account.Equals(Account) && order.Symbol.Equals(Symbol);
    }
}