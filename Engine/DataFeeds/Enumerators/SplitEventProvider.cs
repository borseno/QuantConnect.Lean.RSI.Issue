﻿/*
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
 *
*/

using System;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Event provider who will emit <see cref="Split"/> events
    /// </summary>
    public class SplitEventProvider : ITradableDateEventProvider
    {
        // we set the split factor when we encounter a split in the factor file
        // and on the next trading day we use this data to produce the split instance
        private decimal? _splitFactor;
        private decimal _referencePrice;
        private CorporateFactorProvider _factorFile;
        private MapFile _mapFile;
        private SubscriptionDataConfig _config;

        /// <summary>
        /// Initializes this instance
        /// </summary>
        /// <param name="config">The <see cref="SubscriptionDataConfig"/></param>
        /// <param name="factorFileProvider">The factor file provider to use</param>
        /// <param name="mapFileProvider">The <see cref="Data.Auxiliary.MapFile"/> provider to use</param>
        /// <param name="startTime">Start date for the data request</param>
        public void Initialize(
            SubscriptionDataConfig config,
            IFactorFileProvider factorFileProvider,
            IMapFileProvider mapFileProvider,
            DateTime startTime)
        {
            _config = config;
            _mapFile = mapFileProvider.ResolveMapFile(_config);
            _factorFile = factorFileProvider.Get(_config.Symbol) as CorporateFactorProvider;
        }

        /// <summary>
        /// Check for new splits
        /// </summary>
        /// <param name="eventArgs">The new tradable day event arguments</param>
        /// <returns>New split event if any</returns>
        public IEnumerable<BaseData> GetEvents(NewTradableDateEventArgs eventArgs)
        {
            if (_config.Symbol == eventArgs.Symbol
                && _factorFile != null
                && _mapFile.HasData(eventArgs.Date))
            {
                var factor = _splitFactor;
                if (factor != null)
                {
                    var close = _referencePrice;
                    if (close == 0)
                    {
                        throw new InvalidOperationException($"Zero reference price for {_config.Symbol} split at {eventArgs.Date}");
                    }

                    _splitFactor = null;
                    _referencePrice = 0;
                    yield return new Split(
                        eventArgs.Symbol,
                        eventArgs.Date,
                        close,
                        factor.Value,
                        SplitType.SplitOccurred);
                }

                decimal splitFactor;
                decimal referencePrice;
                if (_factorFile.HasSplitEventOnNextTradingDay(eventArgs.Date, out splitFactor, out referencePrice))
                {
                    _splitFactor = splitFactor;
                    _referencePrice = referencePrice;
                    yield return new Split(
                        eventArgs.Symbol,
                        eventArgs.Date,
                        eventArgs.LastRawPrice ?? 0,
                        splitFactor,
                        SplitType.Warning);
                }
            }
        }
    }
}
