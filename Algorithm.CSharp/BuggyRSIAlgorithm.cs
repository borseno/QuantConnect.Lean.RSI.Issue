/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Brokerages;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    // TODO:
    // 1. check for min order size
    // 2. check for min lot size
    // 3. check check for fees and subtract'em from value.

    /// <summary>
    /// The demonstration algorithm shows some of the most common order methods when working with Crypto assets.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class BuggyRSIAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string BASE = "BTC";
        private const string QUOTE = "USDT";
        private const string SYMBOL = BASE+QUOTE;

        private const Resolution RESOLUTION = Resolution.Hour;

        private RelativeStrengthIndex _rsi;

        public BuggyRSIAlgorithm() {
        }

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetTimeZone(NodaTime.DateTimeZone.Utc);
            SetStartDate(2022, 06, 2); // Set Start Date
            SetEndDate(2022, 06, 30); // Set End Date

            // Set Strategy Cash (USDT)
            SetCash(QUOTE, 10000m);

            // When connected to a real brokerage, the amount specified in SetCash
            // will be replaced with the amount in your actual account.
            SetCash(BASE, 1m);

            SetBrokerageModel(BrokerageName.Binance, AccountType.Cash);
            UniverseSettings.Resolution = RESOLUTION;

            var symbol = AddCrypto(SYMBOL, RESOLUTION).Symbol;

            _rsi = RSI(symbol, 10, MovingAverageType.Simple, RESOLUTION);
        }

        // Sell all ETH holdings with a limit order at 2% above the current price
        private void Sell() {
            var quantity = Portfolio.CashBook[BASE].Amount * 0.99m;
            var limitPrice = Math.Round(Securities[SYMBOL].Price * 0.99m, 2);
            LimitOrder(SYMBOL, -quantity, limitPrice);
        }

        private void Buy(decimal coefficient = 1m) {
            var usdAvailable = USDAvailable() * 0.99m;
            var price = Math.Round(Securities[SYMBOL].Price * 1.01m, 2);
            var quantity = usdAvailable * coefficient / price;
            LimitOrder(SYMBOL, quantity, price);
        }

        private decimal USDAvailable() {
            var usdTotal = Portfolio.CashBook[QUOTE].Amount;
            var usdReserved = Transactions.GetOpenOrders(x => x.Direction == OrderDirection.Buy && x.Type == OrderType.Limit)
                    .Where(x => x.Symbol == SYMBOL)
                    .Sum(x => x.Quantity * ((LimitOrder) x).LimitPrice);
            var usdAvailable = usdTotal - usdReserved;

            return usdAvailable;
        }

        private void Liquidate() {
            // if you want to only liquidate what was bought during algorithm, please use Liquidate() method instead. SetHoldings Liquidates everything, even what was not bought in the algorithm.
            SetHoldings(SYMBOL, 0);
        }

        private void CancelOpenOrders(string symbol) => Transactions.CancelOpenOrders(symbol);

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (Portfolio.CashBook[QUOTE].ConversionRate == 0
                || Portfolio.CashBook[BASE].ConversionRate == 0)
            {
                Log($"QUOTE {QUOTE} conversion rate: {Portfolio.CashBook[QUOTE].ConversionRate}");
                Log($"BASE {BASE} conversion rate: {Portfolio.CashBook[BASE].ConversionRate}");

                throw new Exception("Conversion rate is 0");
            }

            // Expected RSI on 30th Jun using period=10, type=SimpleMovingAverage 20:00 = 25.38

            if (data.Time.Day == 30 && data.Time.Hour == 20) {
                Log($"{data.Time} RSI: ({_rsi})");
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug(Time + " " + orderEvent);
        }

        public override void OnEndOfAlgorithm()
        {
            Log($"{Time} - TotalPortfolioValue: {Portfolio.TotalPortfolioValue}");
            Log($"{Time} - CashBook: {Portfolio.CashBook}");
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 12970;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 240;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "10"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$85.34"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "BTCEUR XJ"},
            {"Fitness Score", "0.5"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "-43.943"},
            {"Portfolio Turnover", "1.028"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "1bf1a6d9dd921982b72a6178f9e50e68"}
        };
    }
}
